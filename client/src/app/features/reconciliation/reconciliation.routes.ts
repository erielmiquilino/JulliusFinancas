import { Routes } from '@angular/router';
import { UnsavedChangesGuard } from '../cards/guards/unsaved-changes.guard';

export const reconciliationRoutes: Routes = [
  {
    path: '',
    title: 'Conciliação Bancária',
    loadComponent: () =>
      import('./components/bank-account-list/bank-account-list.component')
        .then(m => m.BankAccountListComponent)
  },
  {
    path: 'review',
    title: 'Revisão da Conciliação',
    canDeactivate: [UnsavedChangesGuard],
    loadComponent: () =>
      import('./components/reconciliation-review/reconciliation-review.component')
        .then(m => m.ReconciliationReviewComponent)
  },
  {
    path: 'review/:id',
    title: 'Revisão da Conciliação',
    canDeactivate: [UnsavedChangesGuard],
    loadComponent: () =>
      import('./components/reconciliation-review/reconciliation-review.component')
        .then(m => m.ReconciliationReviewComponent)
  }
];
