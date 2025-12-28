---
title: "Core Concepts"
description: "Understand the fundamentals of Entra Auth Cli"
weight: 2
---

# Core Concepts

Understanding these fundamental concepts will help you use Entra Auth Cli effectively.

## Key Topics

### [Authentication Profiles](/docs/core-concepts/profiles/)
Learn what profiles are, how they work, and why they're useful for managing multiple authentication scenarios.

### [OAuth2 Flows](/docs/core-concepts/oauth2-flows/)
Understand the four OAuth2 authentication flows supported by the tool and when to use each one.

### [Scopes & Resources](/docs/core-concepts/scopes/)
Master how scopes define API permissions and learn common scope patterns for Microsoft Graph and custom APIs.

### [Secure Storage](/docs/core-concepts/secure-storage/)
Discover how secrets are protected on each platform and understand the security model.

---

## Quick Overview

**Profiles** store your authentication configuration (tenant, client ID, credentials) so you don't have to enter them repeatedly.

**OAuth2 Flows** determine how authentication happens - some require user interaction, others are fully automated.

**Scopes** specify what resources and permissions your token will have access to.

**Secure Storage** keeps your secrets safe using platform-native encryption (DPAPI on Windows, Keychain on macOS).

---

## Next Steps

Start with [Authentication Profiles](/docs/core-concepts/profiles/) to understand the foundation of how the tool works.
