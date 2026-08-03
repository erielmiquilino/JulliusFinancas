/**
 * Extrai a mensagem de erro da API.
 *
 * Com `[ApiController]`, um `BadRequest(string)` é convertido em ProblemDetails e a mensagem
 * se perde — por isso os controllers da conciliação devolvem `{ message: "..." }`.
 * Esta função cobre esse formato e ainda degrada para os demais, para nunca mostrar
 * "[object Object]" ao usuário.
 */
export function extractApiError(error: unknown): string {
  const body = (error as { error?: unknown })?.error;

  if (typeof body === 'string' && body.trim().length > 0) {
    return body;
  }

  const message = (body as { message?: unknown })?.message;
  if (typeof message === 'string' && message.trim().length > 0) {
    return message;
  }

  const detail = (body as { detail?: unknown })?.detail;
  if (typeof detail === 'string' && detail.trim().length > 0) {
    return detail;
  }

  const httpMessage = (error as { message?: unknown })?.message;
  return typeof httpMessage === 'string' && httpMessage.trim().length > 0
    ? httpMessage
    : 'erro desconhecido';
}
