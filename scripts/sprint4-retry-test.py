#!/usr/bin/env python3
# Mit AI geschrieben.
"""Exercise bounded retries with a real isolated indexing worker and RabbitMQ."""

import base64
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import subprocess
import time
import urllib.error
import urllib.request
import uuid


def main():
    os.chdir(Path(__file__).resolve().parent.parent)
    project = os.environ.get("COMPOSE_PROJECT_NAME", "")
    if (project != "dms-ci" and not project.startswith("dms49-")) or not os.environ.get("COMPOSE_FILE"):
        raise RuntimeError("Select an isolated dms-ci/dms49-* Compose project and COMPOSE_FILE first")
    compose = ["docker", "compose"]
    config = json.loads(subprocess.check_output(compose + ["config", "--format", "json"]))
    env = config["services"]["rabbitmq"]["environment"]
    credentials = base64.b64encode((env["RABBITMQ_DEFAULT_USER"] + ":" + env["RABBITMQ_DEFAULT_PASS"]).encode()).decode()
    base = "http://localhost:15673/api"
    prefix = "dms49-retry-" + uuid.uuid4().hex
    queue, dead_queue, exchange = prefix, prefix + ".dead", prefix + ".dlx"
    container = prefix + "-worker"

    def request(method, path, body=None):
        req = urllib.request.Request(base + path, method=method,
            data=json.dumps(body).encode() if body is not None else None,
            headers={"Content-Type": "application/json", "Authorization": "Basic " + credentials})
        with urllib.request.urlopen(req, timeout=10) as response:
            data = response.read()
            return json.loads(data) if data else None

    try:
        command = compose + ["run", "-d", "--no-deps", "--name", container]
        for name, value in {
            "RabbitMq__IndexQueueName": queue,
            "RabbitMq__IndexRoutingKey": queue,
            "RabbitMq__IndexDeadLetterQueueName": dead_queue,
            "RabbitMq__IndexDeadLetterExchangeName": exchange,
            "RabbitMq__MaxProcessingRetries": "3",
            "RabbitMq__ProcessingRetryDelayMilliseconds": "50",
            # Closed local port: deterministic failure without stopping the shared Elasticsearch.
            "Elasticsearch__Endpoint": "http://127.0.0.1:1",
        }.items():
            command += ["-e", name + "=" + value]
        subprocess.run(command + ["indexing-worker"], check=True, stdout=subprocess.DEVNULL)
        for attempt in range(60):
            try:
                request("GET", "/queues/%2F/" + queue)
                break
            except urllib.error.HTTPError as error:
                if error.code != 404 or attempt == 59:
                    raise
                time.sleep(0.5)
        message_id = str(uuid.uuid4())
        message = {"MessageId": message_id, "DocumentId": str(uuid.uuid4()),
            "FileName": "retry-probe.pdf", "Text": "Retry probe",
            "CompletedAt": datetime.now(timezone.utc).isoformat()}
        published = request("POST", "/exchanges/%2F/dms.events/publish", {
            "properties": {"delivery_mode": 2, "content_type": "application/json", "message_id": message_id},
            "routing_key": queue, "payload": json.dumps(message), "payload_encoding": "string"})
        if not published["routed"]:
            raise RuntimeError("Probe message was not routed to the isolated worker")
        for attempt in range(60):
            messages = request("POST", "/queues/%2F/" + dead_queue + "/get", {
                "count": 1, "ackmode": "ack_requeue_false", "encoding": "auto"})
            if messages:
                break
            time.sleep(0.5)
        if len(messages) != 1:
            raise RuntimeError("Failed job did not reach its dead-letter queue")
        properties = messages[0]["properties"]
        if properties["message_id"] != message_id or properties["headers"].get("x-dms-processing-retries") != 3:
            raise RuntimeError("Retry counter/message identity was not preserved")
        if json.loads(messages[0]["payload"]) != message:
            raise RuntimeError("Retry processing changed the payload")
        print("PASS: real worker retried exactly three times and dead-lettered the unchanged job")
    finally:
        subprocess.run(["docker", "rm", "-f", container], check=False,
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        for path in ["/queues/%2F/" + queue, "/queues/%2F/" + dead_queue, "/exchanges/%2F/" + exchange]:
            try:
                request("DELETE", path)
            except urllib.error.HTTPError as error:
                if error.code != 404:
                    raise


if __name__ == "__main__":
    main()
