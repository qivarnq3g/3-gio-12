#!/usr/bin/env python3
"""Plan by default; --check reads Azure; --apply creates the VM described in issue 145."""
import argparse
import base64
import json
from pathlib import Path
import re
import shlex
import shutil
import subprocess
import sys
import uuid

ROOT = Path(__file__).resolve().parents[1]
QUANG_KEY = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIPtO1TKxah/49QIHhdjTpsmshC3nVmfbxAnv0UFyzRL1 quang-3gio12-deploy"
GROUP, VM, SIZE = "rg-3gio12", "vm-3gio12", "Standard_B1ms"


def public_key(path):
    value = Path(path).expanduser().read_text().strip()
    fields = value.split()
    if len(value.splitlines()) != 1 or len(fields) < 2 or fields[0] != "ssh-ed25519":
        raise ValueError("Provide one Ed25519 .pub key, never a private key.")
    raw = base64.b64decode(fields[1], validate=True)
    if len(raw) != 51 or raw[:19] != b"\x00\x00\x00\x0bssh-ed25519\x00\x00\x00\x20":
        raise ValueError("Invalid Ed25519 public key.")
    if fields[1] == QUANG_KEY.split()[1]:
        raise ValueError("Phuc and Quang must have distinct public keys.")
    return value


def plan(args, key):
    suffix = ["--subscription", args.subscription, "--only-show-errors"]
    return [
        ["az", "group", "create", "-n", GROUP, "-l", args.location, "--tags", "project=3gio12", *suffix],
        ["az", "vm", "create", "-g", GROUP, "-n", VM, "-l", args.location,
         "--image", "Canonical:ubuntu-24_04-lts:server:latest", "--size", SIZE,
         "--storage-sku", "StandardSSD_LRS", "--os-disk-size-gb", "32",
         "--public-ip-sku", "Standard", "--public-ip-address-dns-name", args.dns,
         "--admin-username", "azureuser", "--authentication-type", "ssh",
         "--ssh-key-values", key, QUANG_KEY, "--custom-data", str(ROOT / "deploy/cloud-init.yaml"), *suffix],
        ["az", "vm", "open-port", "-g", GROUP, "-n", VM, "--port", "80", "--priority", "1010", *suffix],
        ["az", "vm", "open-port", "-g", GROUP, "-n", VM, "--port", "443", "--priority", "1020", *suffix],
    ]


def query(args, *command):
    result = subprocess.run(["az", *command, "--subscription", args.subscription,
                             "--only-show-errors", "-o", "json"], check=True, capture_output=True, text=True, timeout=180)
    return json.loads(result.stdout)


def check(args):
    if not shutil.which("az"):
        raise ValueError("Azure CLI not found. Use Azure Cloud Shell or install CLI and run az login.")
    account = query(args, "account", "show")
    if account["id"].lower() != args.subscription.lower() or account["state"] != "Enabled":
        raise ValueError("Selected subscription is not enabled or does not match.")
    if query(args, "group", "exists", "-n", GROUP):
        raise ValueError("Resource group already exists. Inspect it before resuming; no resource was changed.")
    skus = query(args, "vm", "list-skus", "-l", args.location, "--size", SIZE,
                 "--resource-type", "virtualMachines", "--all")
    eligible = [s for s in skus if s["name"] == SIZE and not any(
        r.get("type") == "Location" for r in s.get("restrictions", []))]
    if not eligible:
        raise ValueError("B1ms unavailable/restricted for this subscription and region; no automatic size upgrade.")
    dns = query(args, "network", "public-ip", "check-dns-name", "--domain-name", args.dns, "-l", args.location)
    if not dns.get("available"):
        raise ValueError("DNS label is unavailable; choose another label.")
    usage = query(args, "vm", "list-usage", "-l", args.location)
    family = eligible[0].get("family", "").lower()
    limits = [u for u in usage if u["name"]["value"].lower() in {"cores", family}]
    if len(limits) != 2 or any(u["currentValue"] + 1 > u["limit"] for u in limits):
        raise ValueError("Regional/family vCPU quota missing or insufficient; inspect az vm list-usage.")
    print("Read-only preflight passed: subscription, new group, regional SKU restrictions, DNS and vCPU quota.")
    print("Azure Policy/capacity/credits can still reject provisioning; preflight does not reserve resources.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--subscription", required=True, help="Explicit subscription UUID; never changes the CLI default")
    parser.add_argument("--dns", required=True, help="Unique lowercase Azure DNS label")
    parser.add_argument("--public-key", required=True, help="Phuc's Ed25519 .pub file")
    parser.add_argument("--location", default="malaysiawest")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="Read Azure only; creates nothing")
    mode.add_argument("--apply", action="store_true", help="Run preflight then create billable resources")
    args = parser.parse_args()
    try:
        args.subscription = str(uuid.UUID(args.subscription))
        if not re.fullmatch(r"[a-z][a-z0-9-]{1,61}[a-z0-9]", args.dns):
            raise ValueError("DNS label must be 3–63 lowercase ASCII letters/digits/hyphens, starting with a letter.")
        if not re.fullmatch(r"[a-z][a-z0-9]+", args.location):
            raise ValueError("Use an Azure region identifier, e.g. malaysiawest.")
        commands = plan(args, public_key(args.public_key))
        print(f"Plan: {GROUP}/{VM}, {SIZE}, Ubuntu 24.04, 32 GiB SSD, Standard public IP, SSH keys only.")
        print(f"Expected domain: {args.dns}.{args.location}.cloudapp.azure.com")
        for command in commands:
            print(shlex.join(command))
        if not args.check and not args.apply:
            print("PLAN ONLY: no Azure command executed; no resources created.")
            return 0
        check(args)
        if args.apply:
            for command in commands:
                subprocess.run(command, check=True)
            details = query(args, "vm", "show", "-g", GROUP, "-n", VM,
                            "--query", "{size:hardwareProfile.vmSize,state:provisioningState}")
            print(json.dumps(details, indent=2))
            print("Cloud-init and SSH access still need verification. Quang must confirm his own SSH access.")
        return 0
    except (ValueError, OSError, subprocess.SubprocessError) as error:
        print(f"Provisioning stopped: {error}", file=sys.stderr)
        print("No cleanup/delete is performed. After an apply failure inspect the group before retrying.", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
