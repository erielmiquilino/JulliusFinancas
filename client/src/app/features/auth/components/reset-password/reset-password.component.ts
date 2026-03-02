import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-reset-password',
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
    MatSnackBarModule
  ],
  template: `
    <div class="reset-password-container">
      <div class="reset-password-background">
        <div class="background-overlay"></div>
      </div>

      <div class="reset-password-card-container">
        <!-- Token inválido ou ausente -->
        <mat-card class="reset-password-card" *ngIf="!token">
          <div class="reset-password-header">
            <div class="logo-container">
              <mat-icon class="logo-icon">error_outline</mat-icon>
              <h1>Link Inválido</h1>
            </div>
          </div>
          <mat-card-content class="reset-password-content">
            <p class="error-message">
              O link de redefinição de senha é inválido ou expirou.
              Solicite um novo link na página de recuperação de senha.
            </p>
            <div class="back-to-login">
              <mat-icon>arrow_back</mat-icon>
              <a routerLink="/auth/forgot-password" class="back-link">
                Solicitar novo link
              </a>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Formulário de redefinição -->
        <mat-card class="reset-password-card" *ngIf="token">
          <div class="reset-password-header">
            <div class="logo-container">
              <mat-icon class="logo-icon">lock_reset</mat-icon>
              <h1>Redefinir Senha</h1>
            </div>
          </div>

          <mat-card-content class="reset-password-content">
            <form [formGroup]="resetForm" (ngSubmit)="onSubmit()" class="reset-password-form">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Nova Senha</mat-label>
                <input
                  matInput
                  [type]="hidePassword() ? 'password' : 'text'"
                  formControlName="newPassword"
                  placeholder="Digite sua nova senha"
                >
                <button
                  mat-icon-button
                  matSuffix
                  type="button"
                  (click)="togglePasswordVisibility()"
                >
                  <mat-icon>{{ hidePassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
                <mat-error *ngIf="resetForm.get('newPassword')?.hasError('required')">
                  Nova senha é obrigatória
                </mat-error>
                <mat-error *ngIf="resetForm.get('newPassword')?.hasError('minlength')">
                  Senha deve ter pelo menos 8 caracteres
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Confirmar Senha</mat-label>
                <input
                  matInput
                  [type]="hideConfirmPassword() ? 'password' : 'text'"
                  formControlName="confirmPassword"
                  placeholder="Confirme sua nova senha"
                >
                <button
                  mat-icon-button
                  matSuffix
                  type="button"
                  (click)="toggleConfirmPasswordVisibility()"
                >
                  <mat-icon>{{ hideConfirmPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
                <mat-error *ngIf="resetForm.get('confirmPassword')?.hasError('required')">
                  Confirmação de senha é obrigatória
                </mat-error>
                <mat-error *ngIf="resetForm.get('confirmPassword')?.hasError('passwordMismatch')">
                  As senhas não coincidem
                </mat-error>
              </mat-form-field>

              <button
                mat-raised-button
                color="primary"
                type="submit"
                class="submit-button full-width"
                [disabled]="resetForm.invalid || isSubmitting()"
              >
                <span *ngIf="!isSubmitting()">Redefinir Senha</span>
                <mat-spinner
                  *ngIf="isSubmitting()"
                  diameter="20"
                  color="accent"
                ></mat-spinner>
              </button>
            </form>

            <div class="back-to-login">
              <mat-icon>arrow_back</mat-icon>
              <a routerLink="/auth/login" class="back-link">
                Voltar para o login
              </a>
            </div>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .reset-password-container {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .reset-password-background {
      position: absolute;
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

    .reset-password-card-container {
      position: relative;
      z-index: 2;
      width: 100%;
      max-width: 440px;
      margin: 0 20px;
    }

    .reset-password-card {
      border-radius: 16px;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
      backdrop-filter: blur(10px);
      background: rgba(255, 255, 255, 0.95);
      overflow: hidden;
    }

    .reset-password-header {
      text-align: center;
      padding: 40px 32px 20px;
      background: var(--primary-gradient);
      color: white;
      margin: -24px -24px 0;
    }

    .logo-container {
      display: flex;
      flex-direction: row;
      align-items: center;
      justify-content: center;
      gap: 12px;
      margin-bottom: 12px;
      width: 100%;
    }

    .logo-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
    }

    .reset-password-header h1 {
      margin: 0;
      font-size: 24px;
      font-weight: 600;
    }

    .reset-password-content {
      padding: 32px;
    }

    .reset-password-form {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .full-width {
      width: 100%;
    }

    .error-message {
      text-align: center;
      color: #666;
      font-size: 14px;
      line-height: 1.6;
      margin: 0 0 16px;
    }

    .submit-button {
      height: 48px;
      font-size: 16px;
      font-weight: 600;
      border-radius: 8px;
    }

    .back-to-login {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      margin-top: 24px;
      padding-top: 16px;
      border-top: 1px solid #e0e0e0;
    }

    .back-to-login mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
      color: var(--primary-light);
    }

    .back-link {
      color: var(--primary-light);
      text-decoration: none;
      font-size: 14px;
      font-weight: 500;
      transition: color 0.3s ease;
    }

    .back-link:hover {
      color: var(--primary-color);
      text-decoration: underline;
    }

    @media (max-width: 600px) {
      .reset-password-card-container {
        margin: 0 16px;
        max-width: 100%;
      }

      .reset-password-header {
        padding: 32px 24px 16px;
      }

      .reset-password-content {
        padding: 24px;
      }

      .reset-password-header h1 {
        font-size: 20px;
      }
    }

    @media (max-width: 400px) {
      .reset-password-card-container {
        margin: 0 12px;
      }

      .reset-password-header {
        padding: 24px 20px 12px;
      }

      .reset-password-content {
        padding: 20px;
      }

      .reset-password-header h1 {
        font-size: 18px;
      }
    }

    .submit-button mat-spinner {
      margin: 0 auto;
    }

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
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);

  resetForm!: FormGroup;
  token: string | null = null;
  hidePassword = signal(true);
  hideConfirmPassword = signal(true);
  isSubmitting = signal(false);

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token');
    this.initializeForm();
  }

  private initializeForm(): void {
    this.resetForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('newPassword');
    const confirmPassword = control.get('confirmPassword');

    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    return null;
  }

  togglePasswordVisibility(): void {
    this.hidePassword.set(!this.hidePassword());
  }

  toggleConfirmPasswordVisibility(): void {
    this.hideConfirmPassword.set(!this.hideConfirmPassword());
  }

  onSubmit(): void {
    if (this.resetForm.valid && this.token) {
      this.isSubmitting.set(true);

      this.authService.resetPassword(this.token, this.resetForm.value.newPassword).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.snackBar.open(
            'Senha redefinida com sucesso! Faça login com sua nova senha.',
            'Fechar',
            {
              duration: 5000,
              horizontalPosition: 'center',
              verticalPosition: 'top',
              panelClass: ['success-snackbar']
            }
          );
          this.router.navigate(['/auth/login']);
        },
        error: (error) => {
          this.isSubmitting.set(false);
          const message = error?.error?.message || 'Erro ao redefinir senha. O link pode ter expirado.';
          this.snackBar.open(message, 'Fechar', {
            duration: 5000,
            horizontalPosition: 'center',
            verticalPosition: 'top',
            panelClass: ['error-snackbar']
          });
        }
      });
    }
  }
}
