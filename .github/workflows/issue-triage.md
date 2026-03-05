---
description: Triage new issues with type/provider labels, duplicate detection, and clarification prompts.
on:
  issues:
    types: [opened, edited, reopened]
  workflow_dispatch:
  roles: all
permissions:
  contents: read
  issues: read
  pull-requests: read
tools:
  github:
    toolsets: [default]
    lockdown: false
safe-outputs:
  add-labels:
    allowed:
      - type:build
      - type:ci
      - type:docs
      - type:feat
      - type:fix
      - type:perf
      - type:refactor
      - type:style
      - type:test
      - provider:configcat
      - provider:envvar
      - provider:featuremanagement
      - provider:flagd
      - provider:flagsmith
      - provider:flipt
      - provider:statsig
      - provider:gofeatureflag
      - provider:ofrep
      - duplicate-candidate
      - needs-info
    max: 4
  remove-labels:
    allowed:
      - type:build
      - type:ci
      - type:docs
      - type:feat
      - type:fix
      - type:perf
      - type:refactor
      - type:style
      - type:test
      - provider:configcat
      - provider:envvar
      - provider:featuremanagement
      - provider:flagd
      - provider:flagsmith
      - provider:flipt
      - provider:statsig
      - provider:gofeatureflag
      - provider:ofrep
      - duplicate-candidate
      - needs-info
    max: 4
  add-comment:
    max: 3
  noop:
    max: 1
---

# Issue Triage

You triage newly opened and updated issues.

## Objectives

1. Apply exactly one `type:*` label.
2. Apply at most one `provider:*` label.
3. Identify likely duplicates and provide links.
4. Ask clarifying questions when issue details are incomplete.
5. Suggest the right maintainers in a comment (do not assign users).

## Label Rules

### Type labels (exactly one)

Choose exactly one of these labels:

- `type:build`
- `type:ci`
- `type:docs`
- `type:feat`
- `type:fix`
- `type:perf`
- `type:refactor`
- `type:style`
- `type:test`

Do not apply any `priority:*` labels.

### Provider labels (zero or one)

If a provider is identifiable from title/body, apply exactly one of:

- `provider:configcat`
- `provider:envvar`
- `provider:featuremanagement`
- `provider:flagd`
- `provider:flagsmith`
- `provider:flipt`
- `provider:statsig`
- `provider:gofeatureflag`
- `provider:ofrep`

Normalize casing and synonyms:

- `GO Feature Flag`, `go feature flag`, `gofeatureflag` -> `provider:gofeatureflag`
- `OFREP`, `ofrep` -> `provider:ofrep`
- `feature management`, `FeatureManagement` -> `provider:featuremanagement`

If provider is unclear, do not guess.

## Duplicate Detection

Search open issues for similar title/body terms and the same provider context when available.

If you find likely duplicates:

- Add the `duplicate-candidate` label.
- Post a concise comment with up to 3 candidate issue links and one-line rationale each.
- Do not close the issue automatically.

## Clarification Policy

When report quality is insufficient (missing expected vs actual behavior, repro steps, environment, or provider details):

- Add the `needs-info` label.
- Post focused clarifying questions as a short checklist.
- Pause provider-based maintainer suggestions until enough detail exists.

## Maintainer Suggestions (No Assignment)

Use `.github/component_owners.yml` as source of truth. Suggest owners in a comment only; do not assign users.

Provider-to-component mapping:

- `provider:configcat` -> `src/OpenFeature.Contrib.Providers.ConfigCat`
- `provider:envvar` -> `src/OpenFeature.Contrib.Providers.EnvVar`
- `provider:featuremanagement` -> `src/OpenFeature.Contrib.Providers.FeatureManagement`
- `provider:flagd` -> `src/OpenFeature.Contrib.Providers.Flagd`
- `provider:flagsmith` -> `src/OpenFeature.Contrib.Providers.Flagsmith`
- `provider:flipt` -> `src/OpenFeature.Contrib.Providers.Flipt`
- `provider:statsig` -> `src/OpenFeature.Contrib.Providers.Statsig`
- `provider:gofeatureflag` -> `src/OpenFeature.Providers.GOFeatureFlag`
- `provider:ofrep` -> `src/OpenFeature.Providers.Ofrep`

If you can map provider -> component -> owners, post one concise suggestion comment tagging the owners.

## Update Behavior

Preserve existing unrelated labels. Only change labels required by this workflow.

- Ensure exactly one `type:*` label.
- Ensure at most one `provider:*` label.
- Add `needs-info` and/or `duplicate-candidate` only when criteria are met.

Use `add-labels` and `remove-labels` for label updates, and `add-comment` for public guidance.

## Completion

If no changes are needed and no comment is required, call `noop` with a brief reason.
