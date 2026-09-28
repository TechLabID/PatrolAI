using System.Threading.Tasks;

namespace PatrolAI.NVIDIA
{
    /// <summary>
    /// Provider-neutral reasoning boundary operating only on PatrolAI contracts.
    /// </summary>
    public interface INemotronReasoningService
    {
        Task<PatrolDecision> ReasonAsync(PatrolObservation observation);
    }
}
