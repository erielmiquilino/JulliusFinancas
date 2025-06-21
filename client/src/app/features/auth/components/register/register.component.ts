import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-register',
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
    MatCheckboxModule
  ],
  template: `
    <div class="register-container">
      <div class="register-background">
        <div class="background-overlay"></div>
      </div>

      <div class="register-card-container">
        <mat-card class="register-card">
          <mat-card-header class="register-header">
            <div class="logo-container">
              <mat-icon class="logo-icon">account_balance_wallet</mat-icon>
              <h1>Jullius Finanças</h1>
            </div>
            <p class="welcome-text">Crie sua conta para começar</p>
          </mat-card-header>

          <mat-card-content class="register-content">
            <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" class="register-form">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Nome completo</mat-label>
                <input
                  matInput
                  type="text"
                  formControlName="displayName"
                  placeholder="Digite seu nome completo"
                >
                <mat-icon matSuffix>person</mat-icon>
                <mat-error *ngIf="registerForm.get('displayName')?.hasError('required')">
                  Nome é obrigatório
                </mat-error>
                <mat-error *ngIf="registerForm.get('displayName')?.hasError('minlength')">
                  Nome deve ter pelo menos 2 caracteres
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Email</mat-label>
                <input
                  matInput
                  type="email"
                  formControlName="email"
                  placeholder="Digite seu email"
                >
                <mat-icon matSuffix>email</mat-icon>
                <mat-error *ngIf="registerForm.get('email')?.hasError('required')">
                  Email é obrigatório
                </mat-error>
                <mat-error *ngIf="registerForm.get('email')?.hasError('email')">
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
                <mat-error *ngIf="registerForm.get('password')?.hasError('required')">
                  Senha é obrigatória
                </mat-error>
                <mat-error *ngIf="registerForm.get('password')?.hasError('minlength')">
                  Senha deve ter pelo menos 6 caracteres
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Confirmar senha</mat-label>
                <input
                  matInput
                  [type]="hideConfirmPassword() ? 'password' : 'text'"
                  formControlName="confirmPassword"
                  placeholder="Confirme sua senha"
                >
                <button
                  mat-icon-button
                  matSuffix
                  type="button"
                  (click)="toggleConfirmPasswordVisibility()"
                  [attr.aria-label]="'Hide password'"
                  [attr.aria-pressed]="hideConfirmPassword()"
                >
                  <mat-icon>{{hideConfirmPassword() ? 'visibility_off' : 'visibility'}}</mat-icon>
                </button>
                <mat-error *ngIf="registerForm.get('confirmPassword')?.hasError('required')">
                  Confirmação de senha é obrigatória
                </mat-error>
                <mat-error *ngIf="registerForm.get('confirmPassword')?.hasError('passwordMismatch')">
                  As senhas não coincidem
                </mat-error>
              </mat-form-field>

              <div class="terms-section">
                <mat-checkbox formControlName="acceptTerms" color="primary">
                  <span class="terms-text">
                    Eu aceito os
                    <a href="#" class="terms-link">Termos de Uso</a>
                    e a
                    <a href="#" class="terms-link">Política de Privacidade</a>
                  </span>
                </mat-checkbox>
                <mat-error *ngIf="registerForm.get('acceptTerms')?.hasError('required') && registerForm.get('acceptTerms')?.touched">
                  Você deve aceitar os termos para continuar
                </mat-error>
              </div>

              <button
                mat-raised-button
                color="primary"
                type="submit"
                class="register-button full-width"
                [disabled]="registerForm.invalid || (isLoading$ | async)"
              >
                <span *ngIf="!(isLoading$ | async)">Criar conta</span>
                <mat-spinner
                  *ngIf="isLoading$ | async"
                  diameter="20"
                  color="accent"
                ></mat-spinner>
              </button>
            </form>

            <div class="login-section">
              <p class="login-text">Já tem uma conta?</p>
              <a
                routerLink="/auth/login"
                mat-button
                color="accent"
                class="login-link"
              >
                Fazer login
              </a>
            </div>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .register-container {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      overflow: auto;
      padding: 20px 0;
    }

    .register-background {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: var(--primary-gradient);
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

    .register-card-container {
      position: relative;
      z-index: 2;
      width: 100%;
      max-width: 480px;
      margin: 0 20px;
    }

    .register-card {
      border-radius: 16px;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
      backdrop-filter: blur(10px);
      background: rgba(255, 255, 255, 0.95);
      overflow: hidden;
    }

    .register-header {
      text-align: center;
      padding: 40px 32px 20px;
      background: var(--primary-gradient);
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

    .register-header h1 {
      margin: 0;
      font-size: 24px;
      font-weight: 600;
    }

    .welcome-text {
      margin: 0;
      opacity: 0.9;
      font-size: 14px;
    }

    .register-content {
      padding: 32px;
    }

    .register-form {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .full-width {
      width: 100%;
    }

    .terms-section {
      margin: -8px 0 8px;
    }

    .terms-text {
      font-size: 14px;
      line-height: 1.4;
    }

    .terms-link {
      color: var(--primary-light);
      text-decoration: none;
      font-weight: 500;
    }

    .terms-link:hover {
      color: var(--primary-color);
      text-decoration: underline;
    }

    .register-button {
      height: 48px;
      font-size: 16px;
      font-weight: 600;
      border-radius: 8px;
      margin-top: 8px;
    }

    .login-section {
      text-align: center;
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-top: 24px;
    }

    .login-text {
      margin: 0;
      color: #666;
      font-size: 14px;
    }

    .login-link {
      font-weight: 600;
    }

    /* Error message for checkbox */
    mat-error {
      font-size: 12px;
      color: #f44336;
      margin-top: 4px;
      display: block;
    }

    /* Responsive */
    @media (max-width: 480px) {
      .register-card-container {
        margin: 0 16px;
      }

      .register-header {
        padding: 32px 24px 16px;
      }

      .register-content {
        padding: 24px;
      }

      .logo-icon {
        font-size: 28px;
        width: 28px;
        height: 28px;
      }

      .register-header h1 {
        font-size: 20px;
      }
    }

    /* Loading spinner inside button */
    .register-button mat-spinner {
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
export class RegisterComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);

  registerForm!: FormGroup;
  hidePassword = signal(true);
  hideConfirmPassword = signal(true);
  isLoading$ = this.authService.isLoading();

  ngOnInit(): void {
    this.initializeForm();
    this.subscribeToErrors();
  }

  private initializeForm(): void {
    this.registerForm = this.fb.group({
      displayName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
      acceptTerms: [false, Validators.requiredTrue]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  private passwordMatchValidator(control: AbstractControl): {[key: string]: any} | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }

    if (confirmPassword?.hasError('passwordMismatch')) {
      const errors = confirmPassword.errors;
      delete errors!['passwordMismatch'];
      confirmPassword.setErrors(Object.keys(errors!).length > 0 ? errors : null);
    }

    return null;
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

  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword.set(!this.hideConfirmPassword());
  }

  onSubmit(): void {
    if (this.registerForm.valid) {
      const { email, password, displayName } = this.registerForm.value;

      this.authService.register({ email, password, displayName }).subscribe({
        next: () => {
          this.snackBar.open('Conta criada com sucesso!', 'Fechar', {
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
    Object.keys(this.registerForm.controls).forEach(key => {
      const control = this.registerForm.get(key);
      control?.markAsTouched();
    });
  }
}
