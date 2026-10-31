#!/usr/bin/env python3
"""Validate emitted Unity JSONL against the repository's OpenAPI telemetry contract.

Development dependencies: PyYAML, jsonschema. No network requests are made.
"""
import argparse
import json
from pathlib import Path
import subprocess

import jsonschema
import yaml


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('events', type=Path, help='JSONL exported by NightOneReviewRegression')
    parser.add_argument('--openapi', type=Path, help='Local OpenAPI YAML; default main file or API-contract branch')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    local = args.openapi or root / 'docs/api/openapi.yaml'
    if args.openapi or local.exists():
        contract = local.read_text(encoding='utf-8')
    else:
        contract = subprocess.check_output(
            ['git', 'show', 'origin/docs/4-api-contract:docs/api/openapi.yaml'], cwd=root, text=True)
    document = yaml.safe_load(contract)
    schema = {'$ref': '#/components/schemas/TelemetryEvent', 'components': document['components']}
    validator = jsonschema.Draft202012Validator(schema, format_checker=jsonschema.FormatChecker())
    packets = [json.loads(line) for line in args.events.read_text(encoding='utf-8').splitlines() if line.strip()]
    if not packets:
        raise ValueError('No events to validate')
    identities, previous = set(), {}
    for packet in packets:
        validator.validate(packet)
        if packet['eventId'] in identities:
            raise ValueError('Duplicate eventId')
        identities.add(packet['eventId'])
        session = packet['sessionId']
        if packet['sequence'] <= previous.get(session, -1):
            raise ValueError('Sequence is not increasing within session')
        previous[session] = packet['sequence']
    print(f'OPENAPI_PAYLOAD_VALIDATION_PASSED: {len(packets)} events')


if __name__ == '__main__':
    main()
