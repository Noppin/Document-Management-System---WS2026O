#!/usr/bin/env python3
# Mit AI geschrieben.
"""Verify an existing test upload and a queued job survive container recreation."""

import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--recreate", action="store_true", required=True,
                        help="Explicitly permit recreation of the isolated test containers")
    parser.parse_args()
    os.chdir(Path(__file__).resolve().parent.parent)
    project = os.environ.get("COMPOSE_PROJECT_NAME", "")
    if project != "dms-ci" and not project.startswith("dms49-"):
        raise RuntimeError("Use COMPOSE_PROJECT_NAME=dms49-check or dms-ci; never run against production")
    if not os.environ.get("COMPOSE_FILE"):
        raise RuntimeError("Select docker-compose.yml:docker-compose.test.yml explicitly")

    compose = ["docker", "compose"]
    # Credentials stay in memory; never echo the resolved Compose configuration.
    config = json.loads(subprocess.check_output(compose + ["config", "--format", "json"]))
    rabbit = config["services"]["rabbitmq"]["environment"]
    rabbit_auth = base64.b64encode(
        (rabbit["RABBITMQ_DEFAULT_USER"] + ":" + rabbit["RABBITMQ_DEFAULT_PASS"]).encode()).decode()
    rabbit_url = os.environ.get("DMS_TEST_RABBITMQ", "http://localhost:15673/api")
    api = os.environ.get("API_URL", "http://localhost:18080/api").rstrip("/")
    keycloak = os.environ.get("KEYCLOAK_URL", "http://localhost:18180").rstrip("/")

    def request(url, method="GET", data=None, headers=None):
        req = urllib.request.Request(url, data=data, method=method, headers=headers or {})
        with urllib.request.urlopen(req, timeout=20) as response:
            return response.read()

    def broker(method, path, body=None):
        raw = request(rabbit_url + path, method,
                      None if body is None else json.dumps(body).encode(),
                      {"Authorization": "Basic " + rabbit_auth, "Content-Type": "application/json"})
        return json.loads(raw) if raw else None

    def token():
        realm = json.loads(Path("keycloak/realm-export.json").read_text())
        user = next(user for user in realm["users"] if user["username"] == "alice")
        password = next(item["value"] for item in user["credentials"] if item["type"] == "password")
        data = urllib.parse.urlencode({"grant_type": "password", "client_id": "frontend",
                                       "username": user["username"], "password": password}).encode()
        return json.loads(request(keycloak + "/realms/dms/protocol/openid-connect/token",
                                  "POST", data, {"Content-Type": "application/x-www-form-urlencoded"}))["access_token"]

    def api_request(path, access_token):
        return request(api + path, headers={"Authorization": "Bearer " + access_token})

    access_token = token()
    documents = json.loads(api_request("/documents", access_token))
    if not documents:
        raise RuntimeError("Run sprint4-smoke-test.sh first; no upload exists to verify")
    document_id = documents[0]["id"]
    digest = hashlib.sha256(api_request(f"/documents/{document_id}/content", access_token)).digest()
    search_ids = {item["id"] for item in json.loads(api_request("/search?query=Hello", access_token))}
    if document_id not in search_ids:
        raise RuntimeError("The reference upload must already be indexed before recreation")

    queue = "dms49-persistence-" + uuid.uuid4().hex
    payload = uuid.uuid4().hex
    broker("PUT", "/queues/%2F/" + queue, {"durable": True, "auto_delete": False, "arguments": {}})
    try:
        published = broker("POST", "/exchanges/%2F/amq.default/publish", {
            "properties": {"delivery_mode": 2}, "routing_key": queue,
            "payload": payload, "payload_encoding": "string"})
        if not published["routed"]:
            raise RuntimeError("Persistent probe message could not be routed")

        subprocess.run(compose + ["up", "--force-recreate", "--no-build", "--wait",
                                  "--wait-timeout", "240", "postgres", "minio", "rabbitmq",
                                  "elasticsearch", "api", "frontend", "ocr-worker",
                                  "indexing-worker", "keycloak"], check=True)

        for attempt in range(40):
            try:
                access_token = token()
                content = api_request(f"/documents/{document_id}/content", access_token)
                break
            except (urllib.error.URLError, ConnectionError, TimeoutError):
                if attempt == 39:
                    raise
                time.sleep(3)
        if hashlib.sha256(content).digest() != digest:
            raise RuntimeError("The original MinIO download changed after container recreation")
        search_ids = {item["id"] for item in json.loads(api_request("/search?query=Hello", access_token))}
        if document_id not in search_ids:
            raise RuntimeError("The original Elasticsearch entry did not survive recreation")
        messages = broker("POST", "/queues/%2F/" + queue + "/get", {
            "count": 1, "ackmode": "ack_requeue_false", "encoding": "auto"})
        if len(messages) != 1 or messages[0]["payload"] != payload:
            raise RuntimeError("The persistent RabbitMQ job did not survive recreation")

        postgres = config["services"]["postgres"]["environment"]
        copies = subprocess.check_output(compose + ["exec", "-T", "postgres", "psql", "-U",
            postgres["POSTGRES_USER"], "-d", postgres["POSTGRES_DB"], "-Atc",
            'SELECT count(*) FROM documents WHERE "Content" IS NOT NULL;'], text=True).strip()
        if copies != "0":
            raise RuntimeError("PDF binary copies remain in PostgreSQL")
        print("PASS: original document metadata, identical MinIO bytes, search entry and queued job survived recreation")
    finally:
        broker("DELETE", "/queues/%2F/" + queue)


if __name__ == "__main__":
    main()
