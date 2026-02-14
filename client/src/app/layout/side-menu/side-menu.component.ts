import { Component, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';

interface MenuItem {
  label: string;
  path: string;
  icon: string;
}

@Component({
  selector: 'app-side-menu',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatListModule,
    MatIconModule
  ],
  templateUrl: './side-menu.component.html',
  styleUrls: ['./side-menu.component.scss']
})
export class SideMenuComponent {
  @Output() itemClick = new EventEmitter<void>();

  menuItems: MenuItem[] = [
    { label: 'Dashboard', path: '/dashboard', icon: 'dashboard' },
    { label: 'Transações', path: '/transactions', icon: 'account_balance_wallet' },
    { label: 'Budgets', path: '/budgets', icon: 'savings' },
    { label: 'Cartões', path: '/cards', icon: 'credit_card' },
    { label: 'Categorias', path: '/categories', icon: 'category' },
    { label: 'Contas Atrasadas', path: '/overdue-accounts', icon: 'money_off' },
    { label: 'Configurações', path: '/settings', icon: 'settings' }
  ];

  onItemClick(): void {
    this.itemClick.emit();
  }
}
