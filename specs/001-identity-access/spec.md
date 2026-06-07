# Feature Specification: Identity & Access

**Feature Branch:** `001-identity-access`
**Status:** Draft
**Created:** 2026-06-05
**Updated:** 2026-06-05
**Covers backlog stories:** US1 (admin login), US4 (create administrators and employees), US5 (employee login)

## Summary

The system authenticates users by email and password and manages their accounts and roles. Every account is associated with an employee; some accounts additionally hold the Administrator role, which grants all employee capabilities plus administrative ones. Because an administrator is also an employee, administrators can plan their own attendance just like anyone else. A seeded DefaultAdmin makes the system usable from first start.

## User Scenarios & Testing

### Primary User Story
As a user, I want to log in with my email and password, so that I can use roomy according to my role.

### Acceptance Scenarios

1. **DefaultAdmin first access**
   - GIVEN a freshly initialized system with a DefaultAdmin seeded from configuration
   - WHEN the DefaultAdmin logs in with the configured credentials
   - THEN access is granted with administrator privileges

2. **Successful login**
   - GIVEN a user with a valid account
   - WHEN they log in with the correct email and password
   - THEN access is granted according to their role

3. **Invalid credentials**
   - WHEN a user logs in with an incorrect email or password
   - THEN access is denied
   - AND the response does not reveal whether the account exists

4. **An employee account is provisioned**
   - GIVEN an administrator hires an employee (the organization-led `HireEmployee` saga, ADR-0025)
   - WHEN the account is provisioned with the Employee role and an initial password
   - THEN the account exists with the Employee role
   - AND that person can log in with their email and the initial password once provisioning completes (eventual consistency, ADR-0025)

5. **Administrator creates another administrator**
   - GIVEN an administrator
   - WHEN they create an account and grant it the Administrator role
   - THEN the account holds the Administrator role

6. **Employee cannot manage accounts**
   - GIVEN an employee
   - WHEN they attempt to create or manage an account
   - THEN the action is rejected as not authorized

7. **Role determines capabilities**
   - GIVEN a logged-in employee, THEN they may perform employee actions only
   - GIVEN a logged-in administrator, THEN they may perform all employee actions plus administrative ones

8. **Administrator plans own attendance**
   - GIVEN a logged-in administrator (who is also an employee)
   - WHEN they plan their own attendance
   - THEN it behaves exactly as for any employee (see `003-attendance`)

9. **Password too short**
   - WHEN an administrator sets an initial password shorter than 8 characters
   - THEN the account is not created and the password is rejected

10. **Logout**
    - WHEN a user logs out
    - THEN their session ends and further actions require logging in again

### Edge Cases
- Two accounts MUST NOT share the same email.
- An incorrect login produces a generic failure that does not distinguish "unknown account" from "wrong password".

## Requirements

### Functional Requirements
- **FR-001:** Every account MUST be associated with an employee. An account holds the Employee role and MAY additionally hold the Administrator role.
- **FR-002:** An administrator MUST be able to perform all employee actions plus administrative ones (managing offices and accounts, and acting on behalf of other employees).
- **FR-003:** The system MUST authenticate a user by email and password before granting access.
- **FR-004:** The system MUST be seeded on initialization with a DefaultAdmin account whose credentials come from configuration (not hard-coded in source), so the system can be administered from first use.
- **FR-005:** An administrator MUST be able to create new accounts and choose whether an account additionally holds the Administrator role.
- **FR-006:** When an account is provisioned (via the organization-led `HireEmployee` saga, ADR-0025), an initial password MUST be set; the new user can then log in with their email and that password once provisioning has completed (eventual consistency — login need not be available synchronously with the hiring action).
- **FR-007:** A user without the Administrator role MUST NOT be able to create or manage accounts.
- **FR-008:** The system MUST reject authentication with invalid credentials and MUST NOT reveal whether the account exists.
- **FR-009:** Account email addresses MUST be unique across the system.
- **FR-010:** A password MUST be at least 8 characters; no further complexity rules apply.
- **FR-011:** A user MUST be able to log out, ending their session.
- **FR-012:** Every account, including administrators, MUST have an employee record and MUST be able to plan their own attendance (see `003-attendance`).

### Key Entities (conceptual)
- **User account** — authenticates by email and password; associated with an employee; holds the Employee role and optionally the Administrator role.
- **Role** — Employee (base, held by every account) and Administrator (optional elevation).
- **DefaultAdmin** — the seeded initial administrator account, configured externally, that exists before any other account is created.

## Resolved Decisions
- Authentication mechanism: **email + password via Keycloak** (self-hosted OIDC) behind the YARP BFF — no tokens in the SPA (ADR-0013). No external/social identity provider in the MVP. The identity context manages accounts and roles and provisions the corresponding Keycloak user; Keycloak owns credential verification.
- Account creation flow: provisioning is the **organization-led `HireEmployee` saga** (ADR-0025); an initial password is set; an invite / self-set flow is deferred.
- DefaultAdmin: seeded into Keycloak from **configuration**, not hard-coded; no forced password change in the MVP (recommended, not required).
- Password policy: **minimum length 8**, no complexity rules.
- Administrator vs. Employee: an administrator **is also an employee** (has an employee record and plans attendance); Administrator is an elevation, not a separate account type.

## Out of Scope (this feature)
- External/social identity providers (federated SSO), two-factor authentication, account lockout, and session-timeout policy. (OIDC itself is in scope via Keycloak, ADR-0013.)
- Self-service password reset and change-own-password.
- Invite / self-set initial-password flow (deferred).
- Forced password change on first login.
- Deactivating or deleting accounts (no backlog story).
- The organizational employee record's attributes (company assignment) — part of the Organization model; this feature covers only the account/role aspect.

## Review & Acceptance Checklist
- [ ] No implementation details (no auth library, token format, or hashing)
- [ ] Every functional requirement is testable
- [ ] Each acceptance scenario maps to one or more requirements
- [ ] Role model (every account is an employee; Administrator is an elevation) is unambiguous
- [ ] No open clarification markers remain
