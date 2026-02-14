import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, from, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import {
  Auth,
  signInWithEmailAndPassword,
  signOut,
  sendPasswordResetEmail,
  onAuthStateChanged,
  User as FirebaseUser,
  UserCredential
} from '@angular/fire/auth';
import { User, LoginCredentials, AuthState } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private authStateSubject = new BehaviorSubject<AuthState>({
    user: null,
    isAuthenticated: false,
    isLoading: false, // Mudando para false por padrão
    error: null
  });

  public authState$ = this.authStateSubject.asObservable();

  constructor(
    private auth: Auth,
    private router: Router
  ) {
    // Define loading como true enquanto verifica o estado inicial
    this.setLoading(true);

    // Escuta mudanças no estado de autenticação
    onAuthStateChanged(this.auth, (firebaseUser: FirebaseUser | null) => {
      const currentState = this.authStateSubject.value;

      if (firebaseUser) {
        const user: User = {
          uid: firebaseUser.uid,
          email: firebaseUser.email,
          displayName: firebaseUser.displayName,
          photoURL: firebaseUser.photoURL,
          emailVerified: firebaseUser.emailVerified,
          isAnonymous: firebaseUser.isAnonymous,
          metadata: {
            creationTime: firebaseUser.metadata.creationTime || undefined,
            lastSignInTime: firebaseUser.metadata.lastSignInTime || undefined
          }
        };

        this.authStateSubject.next({
          ...currentState,
          user,
          isAuthenticated: true,
          isLoading: false,
          error: null
        });
      } else {
        this.authStateSubject.next({
          ...currentState,
          user: null,
          isAuthenticated: false,
          isLoading: false,
          error: null
        });
      }
    }, (error) => {
      console.error('Erro ao verificar estado de autenticação:', error);
      const currentState = this.authStateSubject.value;
      this.authStateSubject.next({
        ...currentState,
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: 'Erro ao verificar autenticação'
      });
    });
  }

  /**
   * Realiza login com email e senha
   */
  login(credentials: LoginCredentials): Observable<UserCredential> {
    this.setLoading(true);
    this.clearError();

    return from(signInWithEmailAndPassword(this.auth, credentials.email, credentials.password)).pipe(
      tap(() => {
        this.setLoading(false);
        this.router.navigate(['/dashboard']);
      }),
      catchError((error) => {
        this.setLoading(false);
        this.setError(this.getErrorMessage(error.code));
        return throwError(() => error);
      })
    );
  }

  /**
   * Realiza logout
   */
  logout(): Observable<void> {
    return from(signOut(this.auth)).pipe(
      tap(() => {
        this.router.navigate(['/auth/login']);
      }),
      catchError((error) => {
        console.error('Erro ao fazer logout:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Envia email de recuperação de senha
   */
  resetPassword(email: string): Observable<void> {
    this.setLoading(true);
    this.clearError();

    return from(sendPasswordResetEmail(this.auth, email)).pipe(
      tap(() => {
        this.setLoading(false);
      }),
      catchError((error) => {
        this.setLoading(false);
        this.setError(this.getErrorMessage(error.code));
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

  /**
   * Define estado de loading
   */
  private setLoading(loading: boolean): void {
    const currentState = this.authStateSubject.value;
    this.authStateSubject.next({
      ...currentState,
      isLoading: loading
    });
  }

  /**
   * Define erro
   */
  private setError(error: string): void {
    const currentState = this.authStateSubject.value;
    this.authStateSubject.next({
      ...currentState,
      error
    });
  }

  /**
   * Limpa erro
   */
  private clearError(): void {
    const currentState = this.authStateSubject.value;
    this.authStateSubject.next({
      ...currentState,
      error: null
    });
  }

  /**
   * Converte códigos de erro do Firebase em mensagens amigáveis
   */
  private getErrorMessage(errorCode: string): string {
    switch (errorCode) {
      case 'auth/user-not-found':
        return 'Usuário não encontrado. Verifique o email informado.';
      case 'auth/wrong-password':
        return 'Senha incorreta. Tente novamente.';
      case 'auth/weak-password':
        return 'A senha deve ter pelo menos 6 caracteres.';
      case 'auth/invalid-email':
        return 'Email inválido. Verifique o formato do email.';
      case 'auth/operation-not-allowed':
        return 'Operação não permitida. Entre em contato com o suporte.';
      case 'auth/user-disabled':
        return 'Esta conta foi desabilitada. Entre em contato com o suporte.';
      case 'auth/too-many-requests':
        return 'Muitas tentativas de login. Tente novamente mais tarde.';
      case 'auth/network-request-failed':
        return 'Erro de conexão. Verifique sua conexão com a internet.';
      default:
        return 'Ocorreu um erro inesperado. Tente novamente.';
    }
  }
}
