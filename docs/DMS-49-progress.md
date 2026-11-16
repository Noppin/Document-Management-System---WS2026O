# DMS-49: konsolidierte Restarbeiten

Branch: `feature/DMS-49/feichtlbauer`, Basis: `origin/dev` (`48c3444`).

## Implementiert

- Fuzzy-Suche (`fuzziness: AUTO`), maximal 200 Zeichen pro Suchanfrage und
  Fehlerantworten für fehlende/ungültige Suchbegriffe.
- Explizite Elasticsearch-Mappings vor der ersten Indexierung. Bestehende,
  dynamisch erzeugte Indizes bleiben kompatibel; die Dokumentidentität ist `_id`.
- Begrenzte Nachrichten-Retries für beide Worker (standardmäßig drei zusätzliche
  Versuche, Verzögerungen 1/2/4 Sekunden). Der Versuchszähler steht in einem
  RabbitMQ-Header und überlebt Worker-Neustarts. Nach Ausschöpfen des Budgets
  landet die Nachricht in der vorhandenen Dead-Letter-Queue. Die Retry-Kopie
  wird persistent und mit Publisher-Confirm veröffentlicht, bevor die ursprüngliche
  Nachricht bestätigt wird. Bei einem Crash sind Duplikate möglich; Indexierung
  bleibt durch die feste Dokument-ID idempotent.
- Neue PDFs liegen ausschließlich in MinIO, nicht zusätzlich in PostgreSQL.
  Die nullable Legacy-Spalte bleibt zur sicheren Datenübernahme zunächst erhalten.
  Der API-Start kopiert vorhandene PDFs nach MinIO und verifiziert die Bytes,
  bevor er die alte Kopie entfernt. Bei einem Fehler stoppt der Start, ohne die
  noch nicht übernommenen Quelldaten zu löschen. Der nächste Start setzt fort.
- Upload-Rollback versucht beide Ressourcen aufzuräumen, auch wenn die ursprüngliche
  Anfrage abgebrochen wurde. Cleanup-Fehler werden mit Dokument-ID protokolliert;
  die ursprüngliche Ausnahme bleibt erhalten.
- MinIO-Bereitschaftsprüfung und persistentes RabbitMQ-Volume mit stabilem Hostnamen.
- Such-Controller-, Such-Client-, Mapping-, Retry-, Indexing-Worker-, Storage- und
  Backfill-Regressionstests; zusätzliche Frontend-Suchtests.
- Separat schaltbare Tests mit echtem Elasticsearch, MinIO und zweiseitigem
  Ghostscript-/Tesseract-OCR sowie ein isolierter Docker-Integrationsjob in CI.

## Lokal prüfen

```sh
dotnet test DocumentManagementSystem.sln --configuration Release
# Im Verzeichnis frontend:
npm ci
npm test
npm run build
```

Service-Integrationstests werden ohne `DMS_INTEGRATION_TESTS=true` ausdrücklich
übersprungen; ein grüner Unit-Testlauf ist kein End-to-End-Nachweis.

Für den isolierten Docker-Test werden Docker Compose >= 2.24.4, Ghostscript und
Tesseract mit englischen Sprachdaten benötigt. Die Ports 18080, 18081, 18180,
19000, 19200, 15673, 15602 und 15045 müssen frei sein.

```sh
export COMPOSE_PROJECT_NAME=dms49-check
export COMPOSE_FILE=docker-compose.yml:docker-compose.test.yml
docker compose up --build --wait --wait-timeout 240
DMS_INTEGRATION_TESTS=true dotnet test DocumentManagementSystem.sln --configuration Release --filter Category=Integration
API_URL=http://localhost:18080/api KEYCLOAK_URL=http://localhost:18180 bash scripts/sprint4-smoke-test.sh
python3 scripts/sprint4-retry-test.py
API_URL=http://localhost:18080/api KEYCLOAK_URL=http://localhost:18180 python3 scripts/sprint4-persistence-test.py --recreate
API_URL=http://localhost:18080/api KEYCLOAK_URL=http://localhost:18180 bash scripts/sprint4-smoke-test.sh
# Nur die eigens angelegten Testcontainer stoppen; Volumes bleiben erhalten:
docker compose down
```

**Vor Übernahme auf bestehende Installationen:** PostgreSQL und MinIO sichern.
Bestehende wartende RabbitMQ-Nachrichten vor einer erstmaligen Container-Neuerstellung
abarbeiten oder sichern; das neue Volume kann Daten aus einem alten Container ohne
Volume nicht rückwirkend übernehmen. Die MinIO-Datenmigration ist absichtlich nicht
automatisch rückwärts ausführbar: Für einen Versionsrollback müssen zuerst die PDFs
aus MinIO wieder in die alte Datenbankstruktur übernommen werden.

Der Persistenztest ist absichtlich auf die isolierten Compose-Projekte `dms-ci`
und `dms49-*` beschränkt. Er prüft nach einer Container-Neuerstellung den Download
eines bereits vorhandenen Dokuments per Byte-Hash, dessen Suchtreffer sowie eine
vorher eingereihte persistente RabbitMQ-Nachricht. Ein weiterer erfolgreicher
Upload allein wäre kein ausreichender Persistenznachweis.

## Lokal bereits geprüft

- 170 Backend-Tests sowie 16 Frontend-Tests erfolgreich; Builds erfolgreich.
- Zwei Tests gegen echtes MinIO/Elasticsearch erfolgreich (Byte-Erhalt,
  Fuzzy-Suche, Update ohne Duplikate).
- Docker-Erststart und Upload–OCR–Indexierung–Suche-Smoke-Test erfolgreich.
- Zweiseitiges PDF durch die echte Worker-Pipeline verarbeitet; erwarteter Text
  aus beiden Seiten im Index nachgewiesen.
- Nach Container-Neuerstellung blieben PostgreSQL-Metadaten, Elasticsearch-Einträge
  und eine wartende persistente RabbitMQ-Nachricht erhalten.
- Der automatisierte Persistenztest prüft zusätzlich den unveränderten Download
  aus MinIO nach Neuerstellung aller neun erforderlichen Container.
- Ein isolierter echter Indexing-Worker wurde mit nicht erreichbarem Suchdienst
  geprüft: genau drei zusätzliche Verarbeitungsversuche, unveränderte Nachricht
  einschließlich Message-ID und anschließend Dead-Letter-Ablage. Der gemeinsame
  Elasticsearch-Dienst wird dabei nicht angehalten.

Der lokale .NET-Test mit nativer OCR ist ohne `DMS_INTEGRATION_TESTS=true` explizit
übersprungen; der zweiseitige OCR-Nachweis erfolgte durch die Docker-Worker-Pipeline.

## Noch extern abzunehmen

- GitHub-Branch-Regeln: Backend-, Frontend- und Docker-Integrationsjob als
  verpflichtende Statusprüfungen festlegen. Workflow-Dateien allein erzwingen das nicht.
- Erfolgreichen GitHub-CI-/Release-Lauf und vollständige ZIP-Abgabe nachweisen.
- Die lokal geprüften Erststart-/Persistenzabläufe zusätzlich im GitHub-CI-Job
  und auf der vorgesehenen Zielumgebung abnehmen.
- Nach erfolgreicher Prüfung nach `dev`/`main` integrieren. Alte Branches erst nach
  Abgabe und abgeschlossenem Lektor-Review bereinigen.

Diese Liste ist kein Nachweis für bereits auf GitHub ausgeführte CI-Läufe oder
eine produktive HTTPS-Konfiguration.
