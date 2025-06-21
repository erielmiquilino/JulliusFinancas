import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { UserMenuComponent } from '../../shared/components/user-menu/user-menu.component';
import { AuthService } from '../../core/auth/services/auth.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    UserMenuComponent
  ],
  template: `
    <mat-toolbar color="primary" class="header-toolbar">
      <div class="toolbar-content">
        <div class="brand-section">
          <mat-icon class="brand-icon">account_balance_wallet</mat-icon>
          <span class="brand-text">Jullius Finanças</span>
        </div>

        <div class="spacer"></div>

        <div class="user-section" *ngIf="isAuthenticated$ | async">
          <app-user-menu></app-user-menu>
        </div>
      </div>
    </mat-toolbar>
  `,
  styles: [`
    .header-toolbar {
      position: sticky;
      top: 0;
      z-index: 1000;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      background: var(--primary-gradient);
    }

    .toolbar-content {
      display: flex;
      align-items: center;
      width: 100%;
      height: 100%;
    }

    .brand-section {
      display: flex;
      align-items: center;
      gap: 12px;
      color: white;
    }

    .brand-icon {
      font-size: 28px;
      width: 28px;
      height: 28px;
    }

    .brand-text {
      font-size: 20px;
      font-weight: 600;
      letter-spacing: 0.5px;
    }

    .spacer {
      flex: 1;
    }

    .user-section {
      display: flex;
      align-items: center;
    }

    /* Responsive */
    @media (max-width: 768px) {
      .brand-text {
        font-size: 18px;
      }

      .brand-icon {
        font-size: 24px;
        width: 24px;
        height: 24px;
      }
    }

    @media (max-width: 480px) {
      .brand-text {
        display: none;
      }
    }
  `]
})
export class HeaderComponent {
  private authService = inject(AuthService);

  isAuthenticated$: Observable<boolean> = this.authService.isAuthenticated();
}
