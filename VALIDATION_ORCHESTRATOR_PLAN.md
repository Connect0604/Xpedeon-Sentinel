# Xpedeon Validation Orchestrator - Implementation Plan

**Date:** May 7, 2026  
**Status:** Startup Phase  
**Project:** Xpedeon Construction ERP - WinForms to Blazor Migration Validation

---

## Executive Summary

This document outlines the strategy for building an **AI-powered Validation Orchestrator** that will comprehensively test and validate the migration of Xpedeon Construction ERP from WinForms/C# to Blazor + gRPC + EF Core.

**Core Challenge:** The migration contains undocumented business logic and custom calculations (tribal knowledge) that could be missed. Current validation only covers a basic rule matrix.

**Solution:** Deploy a multi-phase Claude-orchestrated validation system that:
- Discovers hidden business logic from legacy code
- Auto-generates comprehensive test cases
- Executes tests on both systems in parallel
- Identifies all discrepancies
- Provides risk assessment for 1000+ user rollout

---

## Problem Statement

### Current State
- ✅ Xpedeon migration **IN PROGRESS**
- ✅ All modules affected (Procurement, Inventory, Accounting, Plant Management, HR, Subcontractors)
- ✅ Claude orchestrator already generating/migrating code
- ❌ **Validation is incomplete** - only basic rule matrix exists
- ❌ **Unknown unknowns** - undocumented business logic in legacy system
- ❌ **Hard custom logic** prevalent and poorly documented
- ❌ **Test coverage gaps** - existing tests insufficient

### Risk Factors
1. **Tribal Knowledge Risk:** Years of custom logic not documented in code
2. **Time Pressure:** Days to validate (not weeks)
3. **Zero Tolerance:** 100% validation required - any errors will affect 1000+ users
4. **Scope:** Enterprise-wide ERP touching all business functions
5. **Financial Risk:** Construction accounting is highly regulated (project-based financials, vendor payments, auditing)

### Stakeholders
- Finance team (project billing, cost tracking, P&L accuracy)
- Operations (inventory, plant management, multi-site coordination)
- Procurement team (vendor management, approvals, ordering)
- Site managers (real-time updates, mobile access)
- HR (payroll, employee management)
- Executives (performance, security, compliance)

---

## Solution Architecture

### Validation Orchestrator Components

```
┌─────────────────────────────────────────────────────────────┐
│           VALIDATION ORCHESTRATOR (Claude-powered)           │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   DISCOVERY  │  │ TEST FACTORY │  │  EXECUTORS   │      │
│  │              │  │              │  │              │      │
│  │ • Analyze    │  │ • Generate   │  │ • Run old    │      │
│  │   legacy     │  │   test cases │  │   system     │      │
│  │   code       │  │ • Create     │  │ • Run new    │      │
│  │ • Extract    │  │   scenarios  │  │   system     │      │
│  │   patterns   │  │ • Edge cases │  │ • Compare    │      │
│  │ • Find       │  │ • Data sets  │  │   results    │      │
│  │   hidden     │  │              │  │              │      │
│  │   logic      │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│         ↓                 ↓                  ↓               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │        COMPARISON & VALIDATION ENGINE                │  │
│  │ • Identify discrepancies                             │  │
│  │ • Categorize by severity (critical/high/medium/low)  │  │
│  │ • Generate evidence & examples                       │  │
│  │ • Calculate risk score per module                    │  │
│  └──────────────────────────────────────────────────────┘  │
│         ↓                                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         EXPERT VALIDATION INTERFACE                  │  │
│  │ • Present findings to domain experts                 │  │
│  │ • Collect expert feedback                            │  │
│  │ • Validate discovered logic                          │  │
│  │ • Confirm edge cases                                 │  │
│  └──────────────────────────────────────────────────────┘  │
│         ↓                                                    │
│  ┌──────────────────────────────────────────────────────┐  │
│  │          REPORTING & RISK ASSESSMENT                 │  │
│  │ • Comprehensive validation report                    │  │
│  │ • Module-by-module risk scores                       │  │
│  │ • Critical findings flagged                          │  │
│  │ • Go/No-Go recommendation                            │  │
│  │ • Sign-off documentation                             │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Key Features

1. **Automated Discovery**
   - Parse legacy WinForms C# code
   - Extract business logic patterns
   - Identify custom calculations & workflows
   - Discover edge cases and validation rules

2. **Intelligent Test Generation**
   - Create test cases from discovered logic
   - Generate synthetic data matching production patterns
   - Cover happy paths AND edge cases
   - Multi-scenario testing (single user, concurrent, batch)

3. **Parallel Execution**
   - Run tests against legacy WinForms system
   - Run identical tests against Blazor system
   - Compare results in real-time

4. **Smart Comparison**
   - Identify exact discrepancies
   - Categorize by severity
   - Flag critical business logic gaps
   - Generate evidence with examples

5. **Expert Validation Loop**
   - Present findings to domain experts
   - Get confirmation on discovered logic
   - Refine understanding of edge cases
   - Build confidence in migration

---

## Validation Framework

### Validation Dimensions

#### 1. **Financial Accuracy** (CRITICAL)
- Project-based cost tracking
- Billing calculations
- Variance analysis
- Plant hire invoicing
- Vendor payment processing
- P&L accuracy across projects
- Tax and compliance calculations

#### 2. **Inventory Consistency** (CRITICAL)
- Stock levels across multiple sites
- Material tracking and allocation
- Tool and equipment inventory
- Inventory movement transactions
- Stock reconciliation
- Min/max thresholds and alerts

#### 3. **Workflow Integrity** (CRITICAL)
- Procurement approval workflows
- Purchase order generation and tracking
- Subcontractor management
- Invoice matching and approval
- State transitions and validations
- Audit trails

#### 4. **Data Integrity** (CRITICAL)
- No data loss in migration
- Referential integrity maintained
- Date/time precision preserved
- Numerical precision (especially money)
- Text encoding and special characters
- Deletion cascades working correctly

#### 5. **Security & Access Control** (CRITICAL)
- Role-based access enforcement
- User permissions translated correctly
- Multi-site access restrictions
- Data isolation per organization
- Audit logging consistent
- Encryption keys/secrets handled properly

#### 6. **Performance** (HIGH)
- Query performance baseline
- Real-time data synchronization
- Batch job execution time
- Report generation speed
- Load handling (concurrent users)
- Database query optimization

#### 7. **Reporting** (HIGH)
- Financial reports accuracy
- Project management dashboards
- Procurement analytics
- Inventory reports
- Payroll reports
- Custom report logic

#### 8. **Integration Points** (HIGH)
- External API calls (if any)
- Third-party tool integrations
- Export/import functionality
- Data feeds to other systems
- Webhook handling (if applicable)

#### 9. **Business Rules** (HIGH)
- Approval thresholds
- Discount calculations
- Tax calculations
- Currency conversions
- Depreciation schedules (if applicable)
- Rounding and precision rules

#### 10. **User Behavior** (MEDIUM)
- UI/UX behavior matches expectations
- Navigation flows work as expected
- Bulk operations function correctly
- Search and filter logic
- Sorting and pagination
- Mobile/responsive behavior

---

## Execution Phases

### Phase 1: Discovery & Planning (Days 1-2)
**Goal:** Understand what needs to be validated

**Activities:**
1. Analyze legacy WinForms source code
   - Identify all business logic components
   - Extract calculation formulas
   - Find custom validation rules
   - Discover workflow logic
   - Document edge cases

2. Interview domain experts (Finance, Operations, Procurement, HR leads)
   - Confirm discovered logic
   - Identify missing/tribal knowledge
   - Understand critical workflows
   - Get edge case examples from their experience

3. Create validation matrix
   - Map all modules to validation dimensions
   - Prioritize by risk and complexity
   - Identify dependencies
   - Estimate test coverage needed

4. Deliverable: **Discovery Report**
   - Complete inventory of business logic
   - High-level test strategy
   - Risk map by module

---

### Phase 2: Test Case Generation (Days 2-3)
**Goal:** Create comprehensive test suite

**Activities:**
1. Generate test cases for each module
   - Happy path scenarios
   - Edge cases (boundary values, exceptions)
   - Concurrent/multi-user scenarios
   - Data validation scenarios
   - Performance scenarios

2. Create test data
   - Synthetic production-like data
   - Edge case data sets
   - Large volume data (for performance)
   - Multi-site test scenarios

3. Define success criteria
   - Exact match required vs acceptable variance
   - Performance baselines
   - Error margin for floating point math

4. Deliverable: **Comprehensive Test Suite**
   - Test cases per module
   - Test data sets
   - Success criteria document

---

### Phase 3: Parallel Execution (Days 3-4)
**Goal:** Run tests on both systems

**Activities:**
1. Execute test suite on legacy WinForms system
   - Record all outputs
   - Capture performance metrics
   - Document any errors/warnings

2. Execute test suite on new Blazor system
   - Run identical tests
   - Record all outputs
   - Capture performance metrics

3. Both executions run in parallel
   - Minimize time impact
   - Maximize test coverage

4. Deliverable: **Test Execution Results**
   - Results from both systems
   - Performance baselines
   - Execution logs

---

### Phase 4: Comparison & Analysis (Days 4-5)
**Goal:** Identify all discrepancies

**Activities:**
1. Compare results systematically
   - Line-by-line comparison for exact matches
   - Fuzzy matching for acceptable variance
   - Categorize differences by severity

2. Analyze discrepancies
   - Root cause analysis for each difference
   - Classify as: Bug, Missing Logic, Config Issue, Expected Difference
   - Assess impact (affects 1 user, 10%, 50%, all users?)

3. Flag critical issues
   - Any financial calculation mismatch
   - Any workflow logic broken
   - Any data missing
   - Any performance degradation >20%

4. Create discrepancy matrix
   - Module | Test Case | Expected | Actual | Severity | Root Cause | Impact

5. Deliverable: **Detailed Discrepancy Report**
   - All findings categorized
   - Risk assessment per module
   - Evidence and examples

---

### Phase 5: Expert Validation (Days 5-6)
**Goal:** Confirm findings and resolve ambiguities

**Activities:**
1. Present findings to domain experts
   - Show discrepancies with examples
   - Explain detected logic patterns
   - Ask for confirmation/correction

2. Validate discovered business logic
   - Is the extracted logic correct?
   - Are edge cases complete?
   - Are there other edge cases we missed?
   - Get expert sign-off on logic

3. Triage issues
   - Critical blockers must be fixed before go-live
   - High priority should be fixed
   - Medium priority can be fixed post-launch with plan
   - Low priority can be deferred

4. Update findings based on expert feedback
   - Refine risk assessment
   - Adjust severity ratings
   - Document expert sign-off

5. Deliverable: **Validated Findings Report**
   - Expert-reviewed discrepancies
   - Prioritized action items
   - Sign-off documentation

---

### Phase 6: Final Assessment & Reporting (Days 6-7)
**Goal:** Provide go/no-go recommendation

**Activities:**
1. Compile comprehensive validation report
   - Executive summary
   - Module-by-module risk scores
   - Critical findings flagged
   - All discrepancies listed
   - Evidence and examples
   - Remediation recommendations

2. Calculate overall risk score
   - By module (critical, high, medium, low)
   - Overall system readiness percentage
   - Confidence level in validation

3. Go/No-Go analysis
   - Can we launch with current state?
   - What must be fixed before launch?
   - What can we fix post-launch?
   - Recommended launch plan

4. Create sign-off document
   - Stakeholder approval required
   - Conditions for go-live
   - Post-launch validation plan
   - Rollback procedures

5. Deliverable: **Final Validation Report + Go/No-Go Recommendation**

---

## Deliverables

### Phase 1
- [ ] Discovery Report (business logic inventory)
- [ ] Validation Matrix (modules × dimensions)
- [ ] Risk Map by Module

### Phase 2
- [ ] Comprehensive Test Suite (all test cases)
- [ ] Test Data Sets (synthetic data)
- [ ] Success Criteria Document

### Phase 3
- [ ] Legacy System Test Results
- [ ] New System Test Results
- [ ] Performance Baselines
- [ ] Execution Logs

### Phase 4
- [ ] Detailed Discrepancy Report
- [ ] Module Risk Scores
- [ ] Root Cause Analysis
- [ ] Impact Assessment

### Phase 5
- [ ] Validated Findings Report
- [ ] Expert Sign-off Documentation
- [ ] Prioritized Action Items
- [ ] Risk Assessment Update

### Phase 6
- [ ] Final Validation Report
- [ ] Go/No-Go Recommendation
- [ ] Sign-off Documentation
- [ ] Post-Launch Validation Plan

---

## Risk Assessment Matrix

### Module-Level Risks

| Module | Complexity | Logic Clarity | Validation Risk | Priority |
|--------|-----------|---------------|-----------------|----------|
| **Accounting** | Very High | Low (custom) | CRITICAL | P0 |
| **Procurement** | High | Medium | CRITICAL | P0 |
| **Inventory** | High | Medium | HIGH | P1 |
| **Plant Management** | High | Medium | HIGH | P1 |
| **Subcontractors** | Medium | Medium | HIGH | P1 |
| **HR/Payroll** | Very High | Low (custom) | CRITICAL | P0 |
| **Site Management** | Medium | High | MEDIUM | P2 |

### Risk Mitigation Strategies

1. **For Undocumented Logic:**
   - Extract patterns from legacy code automatically
   - Use domain expert interviews to fill gaps
   - Test-driven discovery (if new system behaves differently, we found missing logic)

2. **For Time Pressure:**
   - Run phases in parallel where possible
   - Prioritize highest-risk modules first
   - Use phased validation (launch critical modules first)
   - Automate everything possible

3. **For 100% Validation:**
   - Multiple validation passes (automated + expert)
   - Cross-reference with actual production data
   - Shadow testing of new system before cutover
   - Rollback plan if issues discovered post-launch

---

## Technical Approach

### Technology Stack

```
Validation Orchestrator
├── Claude API (GPT-4 Sonnet) - Orchestration & Analysis
├── Legacy Code Analysis
│   ├── C# Parser (Roslyn)
│   └── Static Analysis Tools
├── Test Execution Framework
│   ├── WinForms Test Runner
│   ├── Blazor/gRPC Test Runner
│   └── Integration Layer
├── Data Comparison Engine
│   ├── Record-by-record comparison
│   ├── Fuzzy matching for floating point
│   └── Performance metrics collection
├── Reporting System
│   ├── Dashboard (discrepancies by module)
│   ├── Report generation (HTML, PDF, JSON)
│   └── Expert feedback interface
└── Database
    ├── Test results storage
    ├── Discrepancy tracking
    └── Audit trail
```

### Implementation Strategy

1. **Phase 1-2:** Build discovery & test generation components
2. **Phase 2-3:** Set up parallel test execution infrastructure
3. **Phase 3-4:** Implement comparison & analysis engine
4. **Phase 4-5:** Create expert validation interface
5. **Phase 5-6:** Generate reports & finalize assessment

### Key Automation Points

- **Code Analysis:** Use Claude to parse legacy code and extract patterns
- **Test Generation:** Generate test cases and data automatically
- **Parallel Execution:** Run tests in parallel (both systems simultaneously)
- **Comparison:** Automated diff analysis with smart matching
- **Reporting:** Auto-generate findings, categorizations, and risk scores

---

## Success Criteria

### Validation Success
- ✅ 100% of documented business logic validated
- ✅ >90% of undocumented logic discovered
- ✅ All critical edge cases identified
- ✅ Zero critical discrepancies found OR all fixed before launch
- ✅ Expert sign-off obtained
- ✅ Risk assessment complete and accepted

### Project Success
- ✅ Validation completed within 7-day timeline
- ✅ Comprehensive report delivered
- ✅ Go/No-Go recommendation made
- ✅ Stakeholders confident in launch decision
- ✅ Post-launch validation plan in place

### Quality Metrics
- ✅ >95% test pass rate (or discrepancies clearly explained)
- ✅ No critical financial calculation errors
- ✅ No data loss detected
- ✅ Performance within 10% of legacy system
- ✅ All workflows execute without errors

---

## Timeline & Milestones

```
Day 1 (May 8)
  ├─ 00:00 - Kickoff: Analysis of legacy code begins
  ├─ 12:00 - Initial business logic patterns extracted
  └─ 23:59 - Milestone: Discovery 50% complete

Day 2 (May 9)
  ├─ 00:00 - Continue analysis + Expert interviews
  ├─ 12:00 - Test case generation starts
  └─ 23:59 - Milestone: Discovery 100%, Test generation 50%

Day 3 (May 10)
  ├─ 00:00 - Test generation continues
  ├─ 12:00 - Parallel test execution starts (both systems)
  └─ 23:59 - Milestone: Test generation 100%, Execution 50%

Day 4 (May 11)
  ├─ 00:00 - Execution continues
  ├─ 12:00 - Comparison & analysis phase begins
  └─ 23:59 - Milestone: Execution 100%, Analysis 50%

Day 5 (May 12)
  ├─ 00:00 - Continue analysis
  ├─ 12:00 - Expert validation phase starts
  └─ 23:59 - Milestone: Analysis 100%, Expert review 50%

Day 6 (May 13)
  ├─ 00:00 - Continue expert validation
  ├─ 12:00 - Report generation starts
  └─ 23:59 - Milestone: Expert review 100%, Reports 50%

Day 7 (May 14)
  ├─ 00:00 - Final reporting & sign-off
  ├─ 12:00 - Go/No-Go decision
  └─ 23:59 - Milestone: DELIVERY - Final report & recommendation
```

---

## Next Steps

### Immediate Actions (Today)
1. ✅ Approve validation orchestrator plan
2. ⬜ Gather legacy WinForms source code
3. ⬜ Confirm domain expert availability for Days 2-5
4. ⬜ Get access to both legacy and new systems for testing
5. ⬜ Prepare test data infrastructure

### Setup (Day 1)
1. Set up code repositories for orchestrator
2. Configure test execution environments
3. Deploy comparison & reporting infrastructure
4. Begin automated code analysis

### Execution (Days 1-7)
Follow the phase-by-phase plan outlined above

---

## Questions & Clarifications

1. **Test Data:** Can we use anonymized production data, or only synthetic?
   - *Current Answer: Synthetic data*

2. **User Acceptance Testing:** When do the 1000+ users start testing?
   - *Timeline coordination needed*

3. **Rollback Plan:** What's the procedure if validation fails?
   - *Need to define rollback SLAs*

4. **Post-Launch Monitoring:** How long will we monitor after go-live?
   - *Recommend: 2-4 weeks with validation orchestrator watching key metrics*

5. **Fixed Issues:** If we find bugs during validation, what's the process?
   - *Define fix → re-validate → sign-off loop*

---

## Document Control

| Version | Date | Author | Status |
|---------|------|--------|--------|
| 1.0 | 2026-05-07 | Claude Code | Draft |
| | | | |

---

**END OF PLAN**
