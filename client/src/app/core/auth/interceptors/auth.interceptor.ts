import { HttpInterceptorFn, HttpHandlerFn, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { BehaviorSubject, throwError, EMPTY } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

/**
 * Chave do access token no localStorage.
 * Duplicada aqui para evitar inject(AuthService) durante a inicialização,
 * o que causaria dependência circular (NG0200) quando o APP_INITIALIZER
 * ainda está construindo o AuthService.
 */
const ACCESS_TOKEN_KEY = 'jullius_access_token';

let isRefreshing = false;
/** null = aguardando, string = novo token, '' = falha no refresh */
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  // Captura o Injector (em vez do AuthService diretamente) para evitar
  // dependência circular durante a construção do AuthService no APP_INITIALIZER.
  // O AuthService é resolvido sob demanda apenas quando necessário (refresh de token em 401).
  const injector = inject(Injector);

  // Não adiciona token em rotas de auth (login, refresh, forgot-password, reset-password)
  if (isAuthRoute(req.url)) {
    return next(req);
  }

  // Lê o token diretamente do localStorage para evitar inject(AuthService)
  const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
  const authReq = accessToken ? addTokenHeader(req, accessToken) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && accessToken) {
        // Neste ponto o AuthService já está totalmente construído
        const authService = injector.get(AuthService);
        return handleUnauthorized(req, next, authService);
      }
      return throwError(() => error);
    })
  );
};

/**
 * Trata erro 401 com mecanismo de fila via BehaviorSubject.
 * Apenas a primeira requisição 401 dispara o refresh;
 * as demais aguardam o novo token na fila.
 */
function handleUnauthorized(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService
) {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      switchMap(response => {
        isRefreshing = false;
        refreshTokenSubject.next(response.accessToken);
        return next(addTokenHeader(req, response.accessToken));
      }),
      catchError(error => {
        isRefreshing = false;
        // Emitir string vazia para desbloquear requisições na fila (sinal de falha)
        refreshTokenSubject.next('');
        // O authService.refreshToken() já chama handleSessionExpired() internamente
        return throwError(() => error);
      })
    );
  }

  // Requisições subsequentes aguardam o novo token
  return refreshTokenSubject.pipe(
    filter(token => token !== null),
    take(1),
    switchMap(token => {
      if (token === '') {
        // Refresh falhou — sessão expirada, descartar requisição silenciosamente
        return EMPTY;
      }
      return next(addTokenHeader(req, token!));
    })
  );
}

function addTokenHeader(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  });
}

function isAuthRoute(url: string): boolean {
  const authPaths = ['/auth/login', '/auth/refresh', '/auth/forgot-password', '/auth/reset-password', '/auth/logout'];
  return authPaths.some(path => url.includes(path));
}
