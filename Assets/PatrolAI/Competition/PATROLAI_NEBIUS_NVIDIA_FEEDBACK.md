# PatrolAI — Nebius and NVIDIA Development Feedback

This feedback records the actual PatrolAI integration experience. It is not sponsored praise and does not claim evaluation of features that were not tested.

## Nebius Token Factory

### What worked well

- Environment-variable authentication and the `/v1/models` discovery request were straightforward once `NEBIUS_API_KEY` was available to the process.
- The OpenAI-compatible `/v1/chat/completions` shape fit a small isolated Unity client.
- Token Factory accepted the strict `json_schema` response format used for the `PatrolDecision` contract.
- The final validation returned HTTP 200 and a usable structured decision from `nvidia/Nemotron-3_5-Lightning`.

### Friction and documentation opportunity

- A successful HTTP response and accepted response format did not by themselves guarantee that decision JSON would appear before the completion limit.
- Diagnosing the initial failures required inspecting `choices[0].message`, `reasoning_content`, and `finish_reason`. The model consumed the 180-token and then 512-token limits in thinking mode and ended with `finish_reason=length` before emitting the final object.
- For short structured decisions, a prominent Token Factory example of model-specific `chat_template_kwargs.enable_thinking=false` would make this behavior easier to diagnose.
- Builder-credit usage and cost visibility were **NOT EVALUATED** in this project, so no claim is made about them.

## NVIDIA Nemotron 3.5 Lightning

### What worked well

- With thinking disabled, the model produced a concise JSON safety decision compatible with the strict schema.
- The validated output meaningfully controlled a bounded Physical AI action instead of serving as decorative text.
- The parser/validator/fallback boundary made the model practical for a safety-oriented prototype: malformed or inconsistent output could not directly command movement.

### Friction and model-specific behavior

- With thinking enabled, both 180 and 512 completion tokens ended at `finish_reason=length` without decision JSON.
- The smallest reliable fix was not a larger context budget or permissive parser. It was keeping the strict schema and adding `chat_template_kwargs.enable_thinking=false` for this short classification-and-action task.
- This behavior should be highlighted in model examples that combine reasoning models with strict structured output.

## Observed runtime

Two successful project validations recorded 1243 ms and 1381 ms end-to-end elapsed times. These are individual observations from the prototype, not a controlled benchmark, average, percentile, or service-level claim.

## Would we use this stack again?

Yes for a similar prototype requiring a hosted NVIDIA model and a narrow structured decision, provided the application keeps strict validation, explicit runtime labels, and a safe fallback. Production use would still require broader reliability, latency, cost, security, and domain-safety evaluation.

