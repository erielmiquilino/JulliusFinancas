import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { Observable } from 'rxjs';
import { User } from '../../../../core/auth/models/user.model';

@Component({
  selector: 'app-dashboard-home',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule
  ],
  template: `
    <div class="dashboard-container">
      <div class="welcome-section" *ngIf="currentUser$ | async as user">
        <h1 class="welcome-title">
          Bem-vindo, {{ user.displayName || 'Usuário' }}!
        </h1>
        <p class="welcome-subtitle">
          Aqui está o resumo das suas finanças
        </p>
      </div>

      <div class="dashboard-cards">
        <mat-card class="dashboard-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>account_balance_wallet</mat-icon>
            <mat-card-title>Saldo Total</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="card-value">R$ 0,00</div>
            <div class="card-description">Em todas as contas</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="dashboard-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>trending_up</mat-icon>
            <mat-card-title>Receitas</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="card-value income">R$ 0,00</div>
            <div class="card-description">Este mês</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="dashboard-card">
          <mat-card-header>
            <mat-icon mat-card-avatar>trending_down</mat-icon>
            <mat-card-title>Despesas</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="card-value expense">R$ 0,00</div>
            <div class="card-description">Este mês</div>
          </mat-card-content>
        </mat-card>
      </div>

      <div class="info-section">
        <mat-card>
          <mat-card-header>
            <mat-icon mat-card-avatar>info</mat-icon>
            <mat-card-title>Autenticação Firebase Configurada!</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p>
              Parabéns! A autenticação com Firebase foi implementada com sucesso.
              Você agora tem acesso a:
            </p>
            <ul>
              <li>Login e registro de usuários</li>
              <li>Recuperação de senha</li>
              <li>Proteção de rotas</li>
              <li>Estado de autenticação reativo</li>
              <li>Interface moderna com Angular Material</li>
            </ul>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .welcome-section {
      margin-bottom: 32px;
      text-align: center;
    }

    .welcome-title {
      font-size: 32px;
      font-weight: 600;
      color: #333;
      margin-bottom: 8px;
    }

    .welcome-subtitle {
      font-size: 16px;
      color: #666;
      margin: 0;
    }

    .dashboard-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 24px;
      margin-bottom: 32px;
    }

    .dashboard-card {
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }

    .dashboard-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12);
    }

    .card-value {
      font-size: 28px;
      font-weight: 700;
      margin: 16px 0 8px;
      color: #333;
    }

    .card-value.income {
      color: #4caf50;
    }

    .card-value.expense {
      color: #f44336;
    }

    .card-description {
      color: #666;
      font-size: 14px;
    }

    .info-section {
      margin-top: 32px;
    }

    .info-section mat-card {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
    }

    .info-section mat-card-content ul {
      margin: 16px 0;
      padding-left: 20px;
    }

    .info-section mat-card-content li {
      margin-bottom: 8px;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .dashboard-container {
        padding: 16px;
      }

      .welcome-title {
        font-size: 24px;
      }

      .dashboard-cards {
        grid-template-columns: 1fr;
        gap: 16px;
      }
    }
  `]
})
export class DashboardHomeComponent {
  private authService = inject(AuthService);

  currentUser$: Observable<User | null> = this.authService.getCurrentUser();
}
