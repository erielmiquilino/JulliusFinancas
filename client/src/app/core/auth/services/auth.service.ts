import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, throwError, of } from 'rxjs';
import { catchError, map, tap, finalize } from 'rxjs/operators';
import {
  User,
  LoginCredentials,
  LoginResponse,
  AuthState,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  ChangePasswordRequest,
  CreateUserRequest
} from '../models/user.model';
import { environment } from '../../../../environments/environment';

const ACCESS_TOKEN_KEY = 'jullius_access_token';
const REFRESH_TOKEN_KEY = 'jullius_refresh_token';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  private authStateSubject = new BehaviorSubject<AuthState>({
    user: null,
    isAuthenticated: false,
    isLoading: true,
    error: null
  });

  public authState$ = this.authStateSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.initializeAuth();
  }

  /**
   * Inicializa o estado de autenticação verificando tokens armazenados
   */
  private initializeAuth(): void {
    const accessToken = this.getAccessToken();
    if (accessToken && !this.isTokenExpired(accessToken)) {
      this.loadCurrentUser();
    } else if (this.getRefreshToken()) {
      this.refreshTokenSilently();
    } else {
      this.setAuthState({ user: null, isAuthenticated: false, isLoading: false, error: null });
    }
  }

  /**
   * Carrega dados do usuário atual via API
   */
  private loadCurrentUser(): void {
    this.http.get<User>(`${this.apiUrl}/me`).pipe(
      catchError(() => {
        this.clearTokens();
        return of(null);
      })
    ).subscribe(user => {
      if (user) {
        this.setAuthState({ user, isAuthenticated: true, isLoading: false, error: null });
      } else {
        this.setAuthState({ user: null, isAuthenticated: false, isLoading: false, error: null });
      }
    });
  }

  /**
   * Tenta renovar o token silenciosamente
   */
  private refreshTokenSilently(): void {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      this.setAuthState({ user: null, isAuthenticated: false, isLoading: false, error: null });
      return;
    }

    this.http.post<LoginResponse>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      catchError(() => {
        this.clearTokens();
        return of(null);
      })
    ).subscribe(response => {
      if (response) {
        this.storeTokens(response.accessToken, response.refreshToken);
        this.setAuthState({
          user: response.user,
          isAuthenticated: true,
          isLoading: false,
          error: null
        });
      } else {
        this.setAuthState({ user: null, isAuthenticated: false, isLoading: false, error: null });
      }
    });
  }

  /**
   * Realiza login com email e senha
   */
  login(credentials: LoginCredentials): Observable<LoginResponse> {
    this.setLoading(true);
    this.clearError();

    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        this.storeTokens(response.accessToken, response.refreshToken);
        this.setAuthState({
          user: response.user,
          isAuthenticated: true,
          isLoading: false,
          error: null
        });
        this.router.navigate(['/dashboard']);
      }),
      catchError((error: HttpErrorResponse) => {
        this.setLoading(false);
        this.setError(this.getErrorMessage(error));
        return throwError(() => error);
      })
    );
  }

  /**
   * Renova o token de acesso usando o refresh token.
   * Retorna Observable<LoginResponse> para uso pelo interceptor.
   */
  refreshToken(): Observable<LoginResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      this.handleSessionExpired();
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<LoginResponse>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      tap(response => {
        this.storeTokens(response.accessToken, response.refreshToken);
        this.setAuthState({
          user: response.user,
          isAuthenticated: true,
          isLoading: false,
          error: null
        });
      }),
      catchError((error: HttpErrorResponse) => {
        this.handleSessionExpired();
        return throwError(() => error);
      })
    );
  }

  /**
   * Realiza logout
   */
  logout(): Observable<void> {
    const refreshToken = this.getRefreshToken();

    return this.http.post<void>(`${this.apiUrl}/logout`, { refreshToken }).pipe(
      finalize(() => {
        this.clearTokens();
        this.setAuthState({ user: null, isAuthenticated: false, isLoading: false, error: null });
        this.router.navigate(['/auth/login']);
      }),
      catchError((error) => {
        console.error('Erro ao fazer logout no servidor:', error);
        return of(void 0);
      })
    );
  }

  /**
   * Envia email de recuperação de senha
   */
  forgotPassword(email: string): Observable<void> {
    this.setLoading(true);
    this.clearError();

    const request: ForgotPasswordRequest = { email };
    return this.http.post<void>(`${this.apiUrl}/forgot-password`, request).pipe(
      tap(() => this.setLoading(false)),
      catchError((error: HttpErrorResponse) => {
        this.setLoading(false);
        this.setError(this.getErrorMessage(error));
        return throwError(() => error);
      })
    );
  }

  /**
   * Reseta a senha com token
   */
  resetPassword(token: string, newPassword: string): Observable<void> {
    this.setLoading(true);
    this.clearError();

    const request: ResetPasswordRequest = { token, newPassword };
    return this.http.post<void>(`${this.apiUrl}/reset-password`, request).pipe(
      tap(() => this.setLoading(false)),
      catchError((error: HttpErrorResponse) => {
        this.setLoading(false);
        this.setError(this.getErrorMessage(error));
        return throwError(() => error);
      })
    );
  }

  /**
   * Altera a senha do usuário autenticado
   */
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    const request: ChangePasswordRequest = { currentPassword, newPassword };
    return this.http.put<void>(`${this.apiUrl}/change-password`, request).pipe(
      catchError((error: HttpErrorResponse) => {
        return throwError(() => error);
      })
    );
  }

  /**
   * Cria um novo usuário (admin only)
   */
  createUser(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>(`${this.apiUrl}/users`, request).pipe(
      catchError((error: HttpErrorResponse) => {
        return throwError(() => error);
      })
    );
  }

  /**
   * Obtém o usuário atual
   */
  getCurrentUser(): Observable<User | null> {
    return this.authState$.pipe(map(state => state.user));
  }

  /**
   * Verifica se o usuário está autenticado
   */
  isAuthenticated(): Observable<boolean> {
    return this.authState$.pipe(map(state => state.isAuthenticated));
  }

  /**
   * Verifica se está carregando
   */
  isLoading(): Observable<boolean> {
    return this.authState$.pipe(map(state => state.isLoading));
  }

  /**
   * Obtém erros de autenticação
   */
  getError(): Observable<string | null> {
    return this.authState$.pipe(map(state => state.error));
  }

  // ==================== Token Management ====================

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  private storeTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }

  private clearTokens(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }

  /**
   * Verifica se o JWT está expirado (decodifica payload base64)
   */
  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expiresAt = payload.exp * 1000;
      // Considera expirado 30s antes para evitar edge cases
      return Date.now() >= expiresAt - 30000;
    } catch {
      return true;
    }
  }

  // ==================== State Helpers ====================

  private setAuthState(state: AuthState): void {
    this.authStateSubject.next(state);
  }

  private setLoading(loading: boolean): void {
    const currentState = this.authStateSubject.value;
    this.authStateSubject.next({ ...currentState, isLoading: loading });
  }

  private setError(error: string): void {
    const currentState = this.authStateSubject.value;
    this.authStateSubject.next({ ...currentState, error });
  }

  private clearError(): void {
    const currentState = this.authStateSubject.value;
    this.authStateSubject.next({ ...currentState, error: null });
  }

  private handleSessionExpired(): void {
    this.clearTokens();
    this.setAuthState({ user: null, isAuthenticated: false, isLoading: false, error: null });
    this.router.navigate(['/auth/login']);
  }

  /**
   * Converte erros HTTP em mensagens amigáveis
   */
  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Erro de conexão. Verifique sua conexão com a internet.';
    }

    // Tenta extrair mensagem do backend
    const backendMessage = error.error?.message || error.error;
    if (typeof backendMessage === 'string' && backendMessage.length < 200) {
      return backendMessage;
    }

    switch (error.status) {
      case 400:
        return 'Dados inválidos. Verifique as informações fornecidas.';
      case 401:
        return 'Email ou senha incorretos.';
      case 403:
        return 'Acesso negado. Você não tem permissão para esta ação.';
      case 404:
        return 'Recurso não encontrado.';
      case 409:
        return 'Conflito. Este email já está em uso.';
      case 429:
        return 'Muitas tentativas. Tente novamente mais tarde.';
      default:
        return 'Ocorreu um erro inesperado. Tente novamente.';
    }
  }
}
