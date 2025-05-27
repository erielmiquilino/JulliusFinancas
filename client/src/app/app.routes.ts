import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadChildren: () => import('./features/dashboard/dashboard.module')
      .then(m => m.DashboardModule)
  },
  {
    path: 'transactions',
    loadChildren: () => import('./features/financial-transaction/financial-transaction.module')
      .then(m => m.FinancialTransactionModule)
  }
];
