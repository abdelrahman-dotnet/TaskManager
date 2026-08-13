using TaskManager.API.Exceptions;
using TaskManager.Bussiness.Authorization;

namespace TaskManager.API.Extentions
{
    /// <summary>
    /// Maps the pipeline's domain exceptions to the API layer's standardized
    /// 404/403 exceptions (NotFoundException / ForbiddenException).
    /// </summary>
    public static class AuthorizationResultExtentions
    {
        public static Exception ToAuthorizationException(this AuthorizationResult result)
        {
            try
            {
                result.ThrowIfFailed();
                return new Exception("AuthorizationResult was successful; this method should only be called on failures.");
            }
            catch (AuthorizationNotFoundException ex)
            {
                return new NotFoundException(ex.Message);
            }
            catch (AuthorizationForbiddenException ex)
            {
                return new ForbiddenException(ex.Message);
            }
        }
    }
}