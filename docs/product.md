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

## Glossary

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
