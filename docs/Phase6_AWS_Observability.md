# VISIONGUARD Phase 6 — AWS Observability and Incident Evidence

## Boundary and architecture

Phase 6 is an additive consumer of a persisted Phase 5 `CONFIRMED_CHANGE` incident, its Phase 4 fusion result, and its Phase 5 temporal result. It does not modify or call detector, ROI, movement, fusion-policy, or temporal-policy code.

The MVP uses only Amazon S3, AWS Lambda, and Amazon CloudWatch Logs. IAM permissions and AWS CloudFormation/SAM are deployment necessities, not runtime data services.

1. `cloud.aws.package` validates the cross-file provenance and writes a local package.
2. `cloud.aws.sync` uploads stable objects below `visionguard/incidents/<incident_id>/` with `package.json` last.
3. S3 invokes the validator Lambda only for keys ending in `package.json`.
4. Lambda validates the schema and package SHA-256, writes the stable `validation.json`, and emits structured JSON to CloudWatch Logs.
5. The client can poll `validation.json`; matching `VALID` evidence changes local state to `SYNCED`.

## Incident package

The package contains only supplied Phase 4/5 facts:

- `schema_version`
- `incident_id`
- `target_id`
- `previous_condition`
- `new_condition`
- `change_id`
- `verification_status`
- `supporting_fusion_ids`
- `supporting_observation_ids`
- `source_ids`
- `timestamps.previous_verified_at`
- `timestamps.confirmed_at`
- `trace_references`
- `supporting_evidence_metadata`
- the original incident, fusion, and temporal source documents
- `package_sha256`, calculated from canonical package JSON

Local artifacts are `incident.json`, `evidence.json`, `fusion.json`, `temporal.json`, `trace.json`, and `package.json`. Their S3 keys are deterministic and use `incident_id` as the logical identity. A retry overwrites the same versioned keys; it does not create another logical incident.

## Local-first sync states

- `LOCAL_ONLY`: validated package exists locally and AWS has not been attempted.
- `SYNC_PENDING`: upload completed or is in progress, but matching Lambda validation has not yet been observed.
- `SYNCED`: Lambda returned `VALID` for the exact `package_sha256`.
- `SYNC_FAILED`: upload failed or Lambda returned a matching invalid result.

Status is written under `exchange/phase6/sync`. Original Phase 5 files are read-only inputs. Failed sync retains both the Phase 5 incident and the Phase 6 local package for retry.

## Credentials and security

The uploader uses the normal boto3 credential provider chain. Configure AWS CLI credentials, environment variables, an AWS profile, IAM Identity Center, or an instance/task role outside this repository. Never put credentials in source, scene, package, or documentation files. S3 public access is blocked and all uploaded objects request S3-managed AES-256 encryption.

## Manual AWS deployment and runtime validation

Prerequisites: AWS CLI, AWS SAM CLI, Python with `boto3`, and an authenticated AWS identity allowed to deploy the stack.

```powershell
aws sts get-caller-identity
sam build --template-file infra/phase6/template.yaml
sam deploy --guided --stack-name visionguard-phase6 --capabilities CAPABILITY_IAM
aws cloudformation describe-stacks --stack-name visionguard-phase6 --query "Stacks[0].Outputs"
```

Use the deployed `IncidentEvidenceBucketName`, then package a real existing Phase 5 confirmed-change incident with its matching results:

```powershell
python -m cloud.aws.sync package --incident exchange/phase5/incidents/<incident_id>.json --fusion exchange/fusion/<fusion_id>.json --temporal exchange/phase5/results/<temporal_id>.json
python -m cloud.aws.sync sync --package-dir exchange/phase6/packages/<incident_id> --bucket <IncidentEvidenceBucketName> --wait-seconds 30
```

Verify the exact cloud objects, validation output, local state, and CloudWatch logs:

```powershell
aws s3 ls s3://<IncidentEvidenceBucketName>/visionguard/incidents/<incident_id>/
Get-Content exchange/phase6/sync/<incident_id>.json
sam logs --stack-name visionguard-phase6 --name IncidentValidatorFunction --tail
```

Run the same `sync` command again. The S3 prefix, package key, validation key, `incident_id`, and `package_sha256` must remain unchanged. S3 versioning may retain transport versions, but there is exactly one logical incident prefix and one current validation record.

AWS runtime is not considered passed until these commands run successfully against a real account and the local status is `SYNCED`.
