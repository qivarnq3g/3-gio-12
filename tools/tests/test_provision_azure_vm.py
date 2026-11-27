import base64
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / "provision_azure_vm.py"


class ProvisionAzureTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        raw = b"\x00\x00\x00\x0bssh-ed25519\x00\x00\x00\x20" + bytes(range(32))
        self.key = self.root / "test.pub"
        self.key.write_text("ssh-ed25519 " + base64.b64encode(raw).decode() + " syntax-test-only")
        self.calls = self.root / "calls.jsonl"
        fake = self.root / "az"
        fake.write_text('''#!/usr/bin/env python3
import json,os,sys
args=sys.argv[1:]
with open(os.environ['AZ_TEST_CALLS'],'a') as f:f.write(json.dumps(args)+'\\n')
scenario=os.environ.get('AZ_TEST_SCENARIO','ok')
if args[:2]==['account','show']: data={'id':'22222222-2222-2222-2222-222222222222' if scenario=='wrong-subscription' else '11111111-1111-1111-1111-111111111111','state':'Disabled' if scenario=='disabled' else 'Enabled'}
elif args[:2]==['group','exists']: data=scenario=='existing'
elif args[:2]==['vm','list-skus']: data=[{'name':'Standard_B1ms','family':'standardBSFamily','restrictions':[{'type':'Location'}] if scenario=='restricted' else []}]
elif args[:3]==['network','public-ip','check-dns-name']: data={'available':scenario!='dns-taken'}
elif args[:2]==['vm','list-usage']: data=[{'name':{'value':n},'currentValue':0,'limit':0 if scenario=='no-quota' else 4} for n in ['cores','standardBSFamily']]
else: data={}
print(json.dumps(data))
''')
        fake.chmod(0o755)
        self.env = os.environ.copy()
        self.env['PATH'] = str(self.root) + os.pathsep + self.env['PATH']
        self.env['AZ_TEST_CALLS'] = str(self.calls)

    def run_script(self, *mode, dns="phuc-3gio12-test", key=None, scenario="ok"):
        env = self.env.copy(); env['AZ_TEST_SCENARIO'] = scenario
        return subprocess.run([sys.executable, str(SCRIPT), '--subscription', '11111111-1111-1111-1111-111111111111',
                               '--dns', dns, '--public-key', str(key or self.key), *mode],
                              env=env, capture_output=True, text=True, timeout=20)

    def read_calls(self):
        return [json.loads(x) for x in self.calls.read_text().splitlines()] if self.calls.exists() else []

    def assert_no_mutations(self):
        for args in self.read_calls():
            self.assertNotIn(args[:2], [['group', 'create'], ['vm', 'create'], ['vm', 'open-port']])

    def test_default_is_offline_plan(self):
        result = self.run_script()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('PLAN ONLY', result.stdout)
        self.assertEqual(self.read_calls(), [])

    def test_check_only_reads_selected_subscription(self):
        result = self.run_script('--check')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assert_no_mutations()
        for args in self.read_calls():
            self.assertEqual(args[args.index('--subscription') + 1], '11111111-1111-1111-1111-111111111111')

    def test_apply_blocked_before_writes(self):
        for scenario in ['wrong-subscription', 'disabled', 'existing', 'restricted', 'dns-taken', 'no-quota']:
            with self.subTest(scenario=scenario):
                self.calls.unlink(missing_ok=True)
                result = self.run_script('--apply', scenario=scenario)
                self.assertNotEqual(result.returncode, 0)
                self.assert_no_mutations()

    def test_explicit_apply_writes_only_after_preflight(self):
        result = self.run_script('--apply')
        self.assertEqual(result.returncode, 0, result.stderr)
        calls = self.read_calls()
        preflight_end = next(i for i, args in enumerate(calls) if args[:2] == ['vm', 'list-usage'])
        mutations = [(i, args[:2]) for i, args in enumerate(calls)
                     if args[:2] in [['group', 'create'], ['vm', 'create'], ['vm', 'open-port']]]
        self.assertEqual(len(mutations), 4)
        self.assertTrue(all(i > preflight_end for i, _ in mutations))
        self.assertFalse(any('delete' in args or args[:2] == ['account', 'set'] for args in calls))

    def test_private_key_is_rejected_without_echoing_secret(self):
        private = self.root / 'private-key'
        private.write_text('-----BEGIN OPENSSH PRIVATE KEY-----\nDO-NOT-ECHO-THIS\n')
        result = self.run_script('--apply', key=private)
        self.assertNotEqual(result.returncode, 0)
        self.assertNotIn('DO-NOT-ECHO-THIS', result.stdout + result.stderr)
        self.assertEqual(self.read_calls(), [])

    def test_invalid_dns_is_rejected_before_azure(self):
        result = self.run_script('--apply', dns='x;touch /tmp/not-allowed')
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.read_calls(), [])


if __name__ == '__main__':
    unittest.main()
