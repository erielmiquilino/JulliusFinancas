import { Component, OnInit, inject } from '@angular/core';
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
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-forgot-password',
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
    <div class="forgot-password-container">
      <div class="forgot-password-background">
        <div class="background-overlay"></div>
      </div>

      <div class="forgot-password-card-container">
        <mat-card class="forgot-password-card">
          <div class="forgot-password-header">
            <div class="logo-container">
              <mat-icon class="logo-icon">lock_reset</mat-icon>
              <h1>Recuperar Senha</h1>
            </div>
          </div>

          <mat-card-content class="forgot-password-content">
            <form [formGroup]="forgotPasswordForm" (ngSubmit)="onSubmit()" class="forgot-password-form">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Email</mat-label>
                <input
                  matInput
                  type="email"
                  formControlName="email"
                  placeholder="Digite seu email"
                  autocomplete="email"
                >
                <mat-icon matSuffix>email</mat-icon>
                <mat-error *ngIf="forgotPasswordForm.get('email')?.hasError('required')">
                  Email é obrigatório
                </mat-error>
                <mat-error *ngIf="forgotPasswordForm.get('email')?.hasError('email')">
                  Digite um email válido
                </mat-error>
              </mat-form-field>

              <button
                mat-raised-button
                color="primary"
                type="submit"
                class="submit-button full-width"
                [disabled]="forgotPasswordForm.invalid || (isLoading$ | async)"
              >
                <span *ngIf="!(isLoading$ | async)">Enviar instruções</span>
                <mat-spinner
                  *ngIf="isLoading$ | async"
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
    .forgot-password-container {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .forgot-password-background {
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

    .forgot-password-card-container {
      position: relative;
      z-index: 2;
      width: 100%;
      max-width: 440px;
      margin: 0 20px;
    }

    .forgot-password-card {
      border-radius: 16px;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
      backdrop-filter: blur(10px);
      background: rgba(255, 255, 255, 0.95);
      overflow: hidden;
    }

    .forgot-password-header {
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

    .forgot-password-header h1 {
      margin: 0;
      font-size: 24px;
      font-weight: 600;
    }

    .description-text {
      margin: 0;
      opacity: 0.9;
      font-size: 14px;
      line-height: 1.4;
      max-width: 280px;
      margin: 0 auto;
    }

    .forgot-password-content {
      padding: 32px;
    }

    .forgot-password-form {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .full-width {
      width: 100%;
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

    /* Responsive - Tablet */
    @media (max-width: 768px) {
      .forgot-password-card-container {
        max-width: 400px;
      }
    }

    /* Responsive - Mobile */
    @media (max-width: 600px) {
      .forgot-password-card-container {
        margin: 0 16px;
        max-width: 100%;
      }

      .forgot-password-header {
        padding: 32px 24px 16px;
      }

      .forgot-password-content {
        padding: 24px;
      }

      .logo-icon {
        font-size: 28px;
        width: 28px;
        height: 28px;
      }

      .forgot-password-header h1 {
        font-size: 20px;
      }

      .forgot-password-form {
        gap: 20px;
      }

      .submit-button {
        height: 44px;
        font-size: 15px;
      }
    }

    /* Responsive - Small Mobile */
    @media (max-width: 400px) {
      .forgot-password-card-container {
        margin: 0 12px;
      }

      .forgot-password-header {
        padding: 24px 20px 12px;
      }

      .forgot-password-content {
        padding: 20px;
      }

      .forgot-password-header h1 {
        font-size: 18px;
      }

      .description-text {
        font-size: 13px;
      }
    }

    /* Loading spinner inside button */
    .submit-button mat-spinner {
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
export class ForgotPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private snackBar = inject(MatSnackBar);

  forgotPasswordForm!: FormGroup;
  isLoading$ = this.authService.isLoading();

  ngOnInit(): void {
    this.initializeForm();
    this.subscribeToErrors();
  }

  private initializeForm(): void {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
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

  onSubmit(): void {
    if (this.forgotPasswordForm.valid) {
      const { email } = this.forgotPasswordForm.value;

      this.authService.forgotPassword(email).subscribe({
        next: () => {
          this.snackBar.open(
            'Instruções de recuperação enviadas para seu email!',
            'Fechar',
            {
              duration: 5000,
              horizontalPosition: 'center',
              verticalPosition: 'top',
              panelClass: ['success-snackbar']
            }
          );

          // Limpa o formulário após sucesso
          this.forgotPasswordForm.reset();
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
    Object.keys(this.forgotPasswordForm.controls).forEach(key => {
      const control = this.forgotPasswordForm.get(key);
      control?.markAsTouched();
    });
  }
}
