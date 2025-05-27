import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FinancialTransaction } from '../../services/financial-transaction.service';

@Component({
  selector: 'app-delete-transaction-dialog',
  template: `
    <h2 mat-dialog-title>Confirmar Exclusão</h2>
    <mat-dialog-content>
      <p>Tem certeza que deseja excluir a transação "{{ data.description }}"?</p>
      <p>Esta ação não pode ser desfeita.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancelar</button>
      <button mat-raised-button color="warn" (click)="onConfirm()">Excluir</button>
    </mat-dialog-actions>
  `
})
export class DeleteTransactionDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<DeleteTransactionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: FinancialTransaction
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
