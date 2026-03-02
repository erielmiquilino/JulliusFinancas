import { HttpInterceptorFn, HttpHandlerFn, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, throwError } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthService);

  // Não adiciona token em rotas de auth (login, refresh, forgot-password, reset-password)
  if (isAuthRoute(req.url)) {
    return next(req);
  }

  const accessToken = authService.getAccessToken();
  const authReq = accessToken ? addTokenHeader(req, accessToken) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && accessToken) {
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
        refreshTokenSubject.next(null);
        return throwError(() => error);
      })
    );
  }

  // Requisições subsequentes aguardam o novo token
  return refreshTokenSubject.pipe(
    filter(token => token !== null),
    take(1),
    switchMap(token => next(addTokenHeader(req, token!)))
  );
}

function addTokenHeader(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  });
}

function isAuthRoute(url: string): boolean {
  const authPaths = ['/auth/login', '/auth/refresh', '/auth/forgot-password', '/auth/reset-password'];
  return authPaths.some(path => url.includes(path));
}
