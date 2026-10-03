# Product context

> DRAFT — written from the code and the author's notes. The author should correct anything that
> doesn't match the intent.

## What this is

MyFinanceAPI is the backend of a personal finance application: accounts, transactions, bank
statement imports, transfers, loans, debt requests, currency conversion, and AI-assisted
classification of expenses. The UI is a separate repo (`myfinance-ui`, Angular) that consumes this API.

It is a solo, non-commercial project. It has a small real user base (the author, family and a friend)
and doubles as the author's sandbox for learning — the stack has been rewritten several times over
~12 years (see [History](#history)). Favor pragmatic, maintainable-by-one-person solutions. Enterprise
concerns (SLAs, uptime monitoring, incident tooling) are not goals.

## Core idea: accounts, sub-accounts, reconciliation

Instead of opening several real bank accounts to separate money mentally (rent, savings, spending),
the user keeps **one real bank account** and does the splitting **inside the app** with accounts and
sub-accounts. At any time the sum of the app's sub-accounts must **reconcile** against the real bank
balance. That is why balance and transaction correctness matters so much: the numbers have to tie back
to a real bank balance.

## Account hierarchy rules

**Structure** (checked on create and edit)
1. An account has at most one main account. Accounts form two levels: a main account can't be a
   sub-account, and an account with sub-accounts can't become one.
2. The main account must belong to the same user.
3. Account groups are independent of the hierarchy: a sub-account can be in a different group than its
   main account.
4. The rules don't depend on the account type (Checking, Saving or Bank). The type only changes what the
   account summary shows.

**Financial entity and currency** (checked on create only, for now)

5. If the main account has a financial entity, the sub-account must have the same one.
6. If the main account has no entity, the sub-account's entity is free.
7. The sub-account may be in a different currency than its main account. Then an exchange method from the
   sub-account's currency to the main account's currency is needed.

**Exchange method** (the server decides and ignores what the client sends, except in the last case)

| Main account has an entity? | Currencies | Method |
|---|---|---|
| Yes | same | the default method (×1) |
| Yes | different | the one method for (sub-account currency → main account currency, that entity). If none exists, the request is rejected. |
| No | same | the default method |
| No | different | the user picks. If only one method exists, it's selected automatically. If none exists, the request is rejected. |

**Existing data and deletion**

8. Existing accounts aren't re-checked on edit. 30 of 80 existing links have a different entity from their
   main account. A one-time script will fix them, and after that rules 5–7 will apply on edit too.
9. Deleting a main account doesn't delete its sub-accounts. They become top-level accounts, and the UI
   warns first.

**Possible future change**

10. A future account type may allow different entities or several main accounts, for a budget spread
    across banks. The rules would then become per account type.

How these are implemented: [architecture.md](architecture.md#account-hierarchy-main-accounts-and-sub-accounts).

## Features

Roughly one per controller in `MyFinanceWebApiCore/Controllers`:

- **Accounts, account groups and periods** — balances, sub-accounts, and per-period views.
- **Spends** — recording transactions that affect a balance (expense *or* income; see Glossary), with
  user-defined spend types/categories.
- **Transfers** — moving money between accounts.
- **Bank transactions and file import** — import bank statements (Excel, one reader per financial
  institution) and match/reconcile them against app transactions.
- **AI-assisted classification** — imported bank transactions are categorized with GPT; results are cached
  in MongoDB so repeat expenses skip the GPT call.
- **Currency conversion** — tailored to Costa Rican banks; uses Banco Central rates per financial entity.
- **Loans.**
- **Debt requests** — mainly for tracking money owed between the author and their spouse. *Planned next
  step (not built yet):* settling a debt should create a real paired transaction on both people's
  accounts, not only flip a status. The creditor/debtor status logic in `DebtRequestService` is
  scaffolding toward that.
- **Scheduled / automatic tasks** — models and endpoints live here (`ScheduledTasks`, `ExecutedTasks`),
  but **execution happens in a separate Azure Functions project outside this repo**. It tracks automatic
  payments/transfers set up at the bank and splits salary across sub-accounts on payday.
- **Users and authentication** — JWT-based.

## AI classification account rules

In the owner's workflow, `Ingresos Ahorros` is used for AI subscriptions despite its name. Other digital
services, such as Azure hosting, belong to `General Bac`. The bank charges digital-service IVA separately
at 13% of the underlying purchase; this tax belongs in the same account as that purchase, not a generic
bank-fee account. For example, a USD 15 Azure charge and its USD 1.95 IVA both go to `General Bac`, while
an AI subscription and its IVA both go to `Ingresos Ahorros`. A generic IVA description alone does not
identify the account: use the associated purchase and amount/currency context.

## Account types

Checking, Saving and Bank only change what the account summary shows. A type can be **inactive**
(`AccountType.IsActive`): it is no longer offered for new accounts, and an account can't be switched to it,
but accounts that already have it keep it and still show it. The add form lists only active types; the edit
form lists the active ones plus the account's current type. The API enforces this too (400 for a new account
or a type change to an inactive type). The flag is changed directly in the database.

## Defaults when creating an account

The add account form fills two fields with **suggestions**, which the user can always change. Both are kept
under a collapsed "Advanced settings" section (in the add and edit forms), together with the default transactions
currency and the "pending by default" switch; the section opens by itself if
one of them has no value.

- **Period type:** the `PeriodDefinition` row flagged `IsDefault` is preselected. The flag is set directly in
  the database (on one row); if none is flagged, nothing is preselected. The edit form ignores it and shows
  the account's own period.
- **Account type:** a new main account suggests **Bank** and a new sub-account suggests **Saving**
  (`AccountTypeSuggestions`, using the account type codes of the `AccountType` enum). The suggestion follows
  whether a main account is chosen, until the user picks a type themselves. An inactive type is never suggested.

The form no longer asks for a **base budget** when creating an account: new accounts always start at 0. The field
is only shown when editing, so existing budgets can still be changed.

## Planned work

- Fix the existing sub-account links whose financial entity differs from their main account's (rule 8 above),
  then apply the entity and exchange-method rules on edit as well as on create.
- Retire the **Checking** account type. It no longer adds anything, but 10 accounts still use it. Account types
  have an `IsActive` flag; the owner sets Checking to inactive in production once this ships (see "Account
  types" above). Nothing else needs to change for those accounts.

## Glossary

- **Main account / sub-account** — accounts form a two-level tree. A sub-account (e.g. a savings bucket)
  rolls up into one main account (e.g. the real bank account). A transaction entered in the sub-account is
  also posted to its main account, converting currency when they differ. See
  [architecture.md](architecture.md#account-hierarchy-main-accounts-and-sub-accounts).

- **Spend** (`Spend`, `SpendType`, `SpendOnPeriod`, `ISpendsService`, …) — a historical name. It means
  *any* transaction that affects an account balance, expense **or** income. Check `AmountType` for
  direction. Don't rename to "Transaction" unless asked.
- **Reconciliation** — matching the sum of sub-accounts against the real bank balance.
- **Financial entity** — a bank/institution. IDs are DB-seeded and referenced as named constants.
- **App transaction vs. bank transaction** — app transactions are what the user records in the app;
  bank transactions are what the bank statement says. Reconciliation connects the two.

## History

Started ~2013 as a personal expense tracker on a home server (WCF + MVC, ADO.NET, stored procedures).
Since then: WCF → Web API, MVC → Angular, ADO.NET → Entity Framework, home server → Azure. Currently
adding MongoDB/Cosmos DB for parts that benefit from a flexible schema. SQL Server + EF remains the
system of record; Mongo is adopted incrementally, not as a replacement.
