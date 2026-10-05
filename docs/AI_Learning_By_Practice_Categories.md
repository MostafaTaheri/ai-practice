# AI Learning by Practice — .NET Architecture Assistant Roadmap

**Prepared for:** Mostafa Taheri — Technical Lead | Software Architect  
**Learning track:** Applied AI engineering for software architecture  
**Starting point:** [Week 1 — Build an AI Architecture Proposal Summarizer with .NET](./Week_01_AI_Architecture_Assistant_DotNet_Tutorial.md)

## Purpose

This roadmap extends the first tutorial into a six-week, practice-based learning track. Each week introduces one AI engineering capability and turns it into a bounded .NET feature. The implementation remains an architecture review assistant rather than a generic chatbot.

The progression moves from a single model call to structured outputs, retrieval, evaluation, tool use, and production-oriented reliability:

```text
Model fundamentals
        ↓
Structured contracts
        ↓
Grounded retrieval
        ↓
Evaluation
        ↓
Bounded tools
        ↓
Observable service
```

## Category list

| Week | Learning focus | Feature to develop in .NET | Implementation scope | Acceptance criteria |
|---:|---|---|---|---|
| 1 | LLM capabilities, context, and limitations | Architecture proposal summarizer | Create `POST /proposals/summarize`. Accept proposal text and call the model through `IArchitectureModelClient`. Request business goals, constraints, components, and unanswered questions. | Three sample proposals produce useful summaries. Missing information is identified. A model timeout returns a controlled error. |
| 2 | Prompting and structured outputs | Structured architecture review | Create `POST /proposals/review`. Return a typed `ArchitectureReview` containing assumptions, risks, alternatives, and recommendations. Validate required fields and risk categories. Allow one repair attempt for invalid model output. | Five proposals deserialize into valid contracts. Unsupported assumptions are labeled. Invalid responses terminate after the retry limit. |
| 3 | Embeddings and RAG | Review against architecture standards | Load 10 short Markdown ADRs or standards. Generate embeddings through a compatible endpoint and store vectors in memory initially. Retrieve relevant passages and attach source IDs to the review. | Five questions retrieve the expected standards. Returned source IDs exist. Missing evidence produces an explicit uncertainty statement. |
| 4 | Evaluation and error analysis | Architecture review regression runner | Build a .NET console runner for 10 fixed cases. Check output validity, source references, and expected risk categories. Export results as JSON. Manually score factual support and usefulness. | One command runs all cases and reports results. A deliberately weakened prompt exposes a regression. Failures can be inspected individually. |
| 5 | Tool use, authorization, and bounded execution | Service-catalog-aware review | Add one read-only tool, `GetServiceDetails(serviceId)`, backed by synthetic service metadata. Validate model-requested arguments, enforce caller access, and allow at most two tool calls. | Reviews incorporate verified service facts. Unknown services and unauthorized access fail safely. Every execution finishes within its call and time budget. |
| 6 | Reliability, observability, and cost | Observable review service with graceful degradation | Add stage-level tracing, elapsed time, token usage where available, and an overall deadline. On model failure, return retrieved standards with an explicit `review unavailable` status. Package the service with Docker. | A request can be traced end to end. Simulated model outage returns the documented fallback. Logs exclude proposal content. The container starts reproducibly. |

## How to use the roadmap

Each week should produce four things:

1. A working feature in the existing .NET solution.
2. A small, fixed set of examples or test cases.
3. Evidence that the acceptance criteria were exercised.
4. Short notes describing model behavior, failure modes, and design trade-offs.

Do not move to the next category only because the endpoint works. The important distinction is between transport correctness and task correctness. A successful HTTP response does not prove that the model was accurate, grounded, authorized, or useful.

## Scope boundaries

- Keep the model behind application-owned interfaces such as `IArchitectureModelClient`.
- Keep deterministic validation, authorization, limits, and fallback behavior in the application code.
- Treat model output as untrusted input.
- Keep retrieved evidence and source identifiers visible in the review result.
- Use synthetic service metadata and local or low-cost infrastructure during practice.
- Avoid adding autonomous writes, unrestricted tool execution, or production customer data to this learning track.

## Completion outcome

At the end of Week 6, the project should demonstrate a bounded AI-assisted architecture review workflow with:

- Structured model output and repair handling.
- Retrieval grounded in architecture standards.
- Repeatable regression cases and error analysis.
- Read-only, authorized tool use with call limits.
- Tracing, cost-related measurements, deadlines, and graceful degradation.
- A reproducible containerized runtime.

This is a learning and portfolio project. Its results should be treated as evidence about the implementation and evaluation cases, not as proof that the model is generally reliable for architecture decisions.
