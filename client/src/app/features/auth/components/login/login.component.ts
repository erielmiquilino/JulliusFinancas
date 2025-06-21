import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDividerModule,
    MatCheckboxModule
  ],
  template: `
    <div class="login-container">
      <div class="login-background">
        <div class="background-overlay"></div>
      </div>

      <div class="login-card-container">
        <mat-card class="login-card">
          <mat-card-header class="login-header">
            <div class="logo-container">
              <mat-icon class="logo-icon">account_balance_wallet</mat-icon>
              <h1>Jullius Finanças</h1>
            </div>
            <p class="welcome-text">Faça login para acessar sua conta</p>
          </mat-card-header>

          <mat-card-content class="login-content">
            <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="login-form">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Email</mat-label>
                <input
                  matInput
                  type="email"
                  formControlName="email"
                  placeholder="Digite seu email"
                >
                <mat-icon matSuffix>email</mat-icon>
                <mat-error *ngIf="loginForm.get('email')?.hasError('required')">
                  Email é obrigatório
                </mat-error>
                <mat-error *ngIf="loginForm.get('email')?.hasError('email')">
                  Digite um email válido
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Senha</mat-label>
                <input
                  matInput
                  [type]="hidePassword() ? 'password' : 'text'"
                  formControlName="password"
                  placeholder="Digite sua senha"
                >
                <button
                  mat-icon-button
                  matSuffix
                  type="button"
                  (click)="togglePasswordVisibility()"
                  [attr.aria-label]="'Hide password'"
                  [attr.aria-pressed]="hidePassword()"
                >
                  <mat-icon>{{hidePassword() ? 'visibility_off' : 'visibility'}}</mat-icon>
                </button>
                <mat-error *ngIf="loginForm.get('password')?.hasError('required')">
                  Senha é obrigatória
                </mat-error>
                <mat-error *ngIf="loginForm.get('password')?.hasError('minlength')">
                  Senha deve ter pelo menos 6 caracteres
                </mat-error>
              </mat-form-field>

              <div class="form-options">
                <mat-checkbox formControlName="rememberMe" color="primary">
                  Lembrar de mim
                </mat-checkbox>

                <a
                  routerLink="/auth/forgot-password"
                  class="forgot-password-link"
                >
                  Esqueceu a senha?
                </a>
              </div>

              <button
                mat-raised-button
                color="primary"
                type="submit"
                class="login-button full-width"
                [disabled]="loginForm.invalid || (isLoading$ | async)"
              >
                <span *ngIf="!(isLoading$ | async)">Entrar</span>
                <mat-spinner
                  *ngIf="isLoading$ | async"
                  diameter="20"
                  color="accent"
                ></mat-spinner>
              </button>
            </form>

            <mat-divider class="divider">
              <span class="divider-text">ou</span>
            </mat-divider>

            <div class="register-section">
              <p class="register-text">Não tem uma conta?</p>
              <a
                routerLink="/auth/register"
                mat-button
                color="accent"
                class="register-link"
              >
                Criar conta
              </a>
            </div>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      overflow: hidden;
    }

    .login-background {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      z-index: 1;
    }

    .background-overlay {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.3);
    }

    .login-card-container {
      position: relative;
      z-index: 2;
      width: 100%;
      max-width: 440px;
      margin: 0 20px;
    }

    .login-card {
      border-radius: 16px;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
      backdrop-filter: blur(10px);
      background: rgba(255, 255, 255, 0.95);
      overflow: hidden;
    }

    .login-header {
      text-align: center;
      padding: 40px 32px 20px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      margin: -24px -24px 0;
    }

    .logo-container {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 12px;
      margin-bottom: 8px;
    }

    .logo-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
    }

    .login-header h1 {
      margin: 0;
      font-size: 24px;
      font-weight: 600;
    }

    .welcome-text {
      margin: 0;
      opacity: 0.9;
      font-size: 14px;
    }

    .login-content {
      padding: 32px;
    }

    .login-form {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .full-width {
      width: 100%;
    }

    .form-options {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin: -8px 0 8px;
    }

    .forgot-password-link {
      color: #667eea;
      text-decoration: none;
      font-size: 14px;
      font-weight: 500;
      transition: color 0.3s ease;
    }

    .forgot-password-link:hover {
      color: #764ba2;
      text-decoration: underline;
    }

    .login-button {
      height: 48px;
      font-size: 16px;
      font-weight: 600;
      border-radius: 8px;
      margin-top: 8px;
      position: relative;
    }

    .divider {
      margin: 24px 0;
      position: relative;
    }

    .divider-text {
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      background: white;
      padding: 0 16px;
      color: #666;
      font-size: 14px;
    }

    .register-section {
      text-align: center;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .register-text {
      margin: 0;
      color: #666;
      font-size: 14px;
    }

    .register-link {
      font-weight: 600;
    }

    /* Responsive */
    @media (max-width: 480px) {
      .login-card-container {
        margin: 0 16px;
      }

      .login-header {
        padding: 32px 24px 16px;
      }

      .login-content {
        padding: 24px;
      }

      .logo-icon {
        font-size: 28px;
        width: 28px;
        height: 28px;
      }

      .login-header h1 {
        font-size: 20px;
      }
    }

    /* Loading spinner inside button */
    .login-button mat-spinner {
      margin: 0 auto;
    }

    /* Form field improvements */
    ::ng-deep .mat-mdc-form-field {
      .mat-mdc-text-field-wrapper {
        border-radius: 8px;
      }
    }

    ::ng-deep .mat-mdc-raised-button {
      border-radius: 8px;
    }
  `]
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);

  loginForm!: FormGroup;
  hidePassword = signal(true);
  isLoading$ = this.authService.isLoading();

  ngOnInit(): void {
    this.initializeForm();
    this.subscribeToErrors();
  }

  private initializeForm(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false]
    });
  }

  private subscribeToErrors(): void {
    this.authService.getError().subscribe(error => {
      if (error) {
        this.snackBar.open(error, 'Fechar', {
          duration: 5000,
          horizontalPosition: 'center',
          verticalPosition: 'top',
          panelClass: ['error-snackbar']
        });
      }
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword.set(!this.hidePassword());
  }

  onSubmit(): void {
    if (this.loginForm.valid) {
      const { email, password } = this.loginForm.value;

      this.authService.login({ email, password }).subscribe({
        next: () => {
          this.snackBar.open('Login realizado com sucesso!', 'Fechar', {
            duration: 3000,
            horizontalPosition: 'center',
            verticalPosition: 'top',
            panelClass: ['success-snackbar']
          });
        },
        error: () => {
          // Erro já é tratado no serviço e exibido via subscription
        }
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.loginForm.controls).forEach(key => {
      const control = this.loginForm.get(key);
      control?.markAsTouched();
    });
  }
}
