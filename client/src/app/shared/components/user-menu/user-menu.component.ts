import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../../core/auth/services/auth.service';
import { Observable } from 'rxjs';
import { User } from '../../../core/auth/models/user.model';

@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [
    CommonModule,
    MatMenuModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatDividerModule
  ],
  template: `
    <button
      mat-icon-button
      [matMenuTriggerFor]="userMenu"
      matTooltip="Menu do usuário"
      class="user-menu-trigger"
    >
      <div class="user-avatar">
        <img
          *ngIf="(currentUser$ | async)?.photoURL; else defaultAvatar"
          [src]="(currentUser$ | async)?.photoURL!"
          [alt]="(currentUser$ | async)?.displayName || 'Avatar do usuário'"
          class="avatar-image"
        >
        <ng-template #defaultAvatar>
          <mat-icon class="default-avatar-icon">account_circle</mat-icon>
        </ng-template>
      </div>
    </button>

    <mat-menu #userMenu="matMenu" class="user-menu">
      <div class="user-info" *ngIf="currentUser$ | async as user">
        <div class="user-avatar-large">
          <img
            *ngIf="user.photoURL; else defaultAvatarLarge"
            [src]="user.photoURL"
            [alt]="user.displayName || 'Avatar do usuário'"
            class="avatar-image-large"
          >
          <ng-template #defaultAvatarLarge>
            <mat-icon class="default-avatar-icon-large">account_circle</mat-icon>
          </ng-template>
        </div>
        <div class="user-details">
          <div class="user-name">{{ user.displayName || 'Usuário' }}</div>
          <div class="user-email">{{ user.email }}</div>
        </div>
      </div>

      <mat-divider></mat-divider>

      <button mat-menu-item class="menu-item">
        <mat-icon>person</mat-icon>
        <span>Meu Perfil</span>
      </button>

      <button mat-menu-item class="menu-item">
        <mat-icon>settings</mat-icon>
        <span>Configurações</span>
      </button>

      <mat-divider></mat-divider>

      <button mat-menu-item class="menu-item logout-item" (click)="logout()">
        <mat-icon>logout</mat-icon>
        <span>Sair</span>
      </button>
    </mat-menu>
  `,
  styles: [`
    .user-menu-trigger {
      padding: 8px;
    }

    .user-avatar {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      overflow: hidden;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--primary-gradient);
    }

    .avatar-image {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .default-avatar-icon {
      color: white;
      font-size: 24px;
      width: 24px;
      height: 24px;
    }

    ::ng-deep .user-menu {
      margin-top: 8px;
      border-radius: 12px;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12);
      padding: 0;
      min-width: 280px;
    }

    .user-info {
      padding: 16px;
      display: flex;
      align-items: center;
      gap: 12px;
      background: var(--primary-gradient);
      color: white;
    }

    .user-avatar-large {
      width: 48px;
      height: 48px;
      border-radius: 50%;
      overflow: hidden;
      display: flex;
      align-items: center;
      justify-content: center;
      background: rgba(255, 255, 255, 0.2);
      flex-shrink: 0;
    }

    .avatar-image-large {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .default-avatar-icon-large {
      color: white;
      font-size: 32px;
      width: 32px;
      height: 32px;
    }

    .user-details {
      flex: 1;
      min-width: 0;
    }

    .user-name {
      font-weight: 600;
      font-size: 16px;
      margin-bottom: 2px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .user-email {
      font-size: 13px;
      opacity: 0.9;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .menu-item {
      padding: 12px 16px;
      font-size: 14px;
      display: flex;
      align-items: center;
      gap: 12px;
      transition: background-color 0.2s ease;
    }

    .menu-item mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
      color: #666;
    }

    .menu-item:hover {
      background-color: #f5f5f5;
    }

    .logout-item {
      color: #f44336;
    }

    .logout-item mat-icon {
      color: #f44336;
    }

    .logout-item:hover {
      background-color: #ffebee;
    }

    ::ng-deep .mat-mdc-menu-content {
      padding: 0 !important;
    }
  `]
})
export class UserMenuComponent {
  private authService = inject(AuthService);

  currentUser$: Observable<User | null> = this.authService.getCurrentUser();

  logout(): void {
    this.authService.logout().subscribe();
  }
}
