import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, from, throwError } from 'rxjs';
import { catchError, map, switchMap, tap } from 'rxjs/operators';
import { AngularFireAuth } from '@angular/fire/compat/auth';
import firebase from 'firebase/compat/app';
import { User, LoginCredentials, RegisterCredentials, AuthState } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private authStateSubject = new BehaviorSubject<AuthState>({
    user: null,
    isAuthenticated: false,
    isLoading: true,
    error: null
  });

  public authState$ = this.authStateSubject.asObservable();

  constructor(
    private afAuth: AngularFireAuth,
    private router: Router
  ) {
    // Escuta mudanças no estado de autenticação
    this.afAuth.authState.subscribe((firebaseUser: firebase.User | null) => {
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
    });
  }

  /**
   * Realiza login com email e senha
   */
  login(credentials: LoginCredentials): Observable<firebase.auth.UserCredential> {
    this.setLoading(true);
    this.clearError();

    return from(this.afAuth.signInWithEmailAndPassword(credentials.email, credentials.password)).pipe(
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
   * Realiza registro de novo usuário
   */
  register(credentials: RegisterCredentials): Observable<firebase.auth.UserCredential> {
    this.setLoading(true);
    this.clearError();

    return from(this.afAuth.createUserWithEmailAndPassword(credentials.email, credentials.password)).pipe(
      switchMap((userCredential) => {
        // Atualiza o perfil com o nome de usuário se fornecido
        if (credentials.displayName && userCredential.user) {
          return from(userCredential.user.updateProfile({
            displayName: credentials.displayName
          })).pipe(
            map(() => userCredential)
          );
        }
        return from([userCredential]);
      }),
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
    return from(this.afAuth.signOut()).pipe(
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

    return from(this.afAuth.sendPasswordResetEmail(email)).pipe(
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
      case 'auth/email-already-in-use':
        return 'Este email já está sendo usado por outra conta.';
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
