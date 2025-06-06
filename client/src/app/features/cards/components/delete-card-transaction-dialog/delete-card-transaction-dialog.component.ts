import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { CardTransaction } from '../../services/card.service';

@Component({
  selector: 'app-delete-card-transaction-dialog',
  template: `
    <h2 mat-dialog-title>Confirmar Exclusão</h2>
    <mat-dialog-content>
      <p>Tem certeza que deseja excluir o lançamento "{{ data.descricao }}"?</p>
      <p class="transaction-details">
        <strong>Valor:</strong> {{ data.valor | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}<br>
        <strong>Data:</strong> {{ data.data | date:'dd/MM/yyyy':'GMT':'pt-BR' }}<br>
        <strong>Parcela:</strong> {{ data.parcela }}
      </p>
      <p class="warning-text">Esta ação não pode ser desfeita.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancelar</button>
      <button mat-raised-button color="warn" (click)="onConfirm()">Excluir</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .transaction-details {
      background-color: #f5f5f5;
      padding: 16px;
      border-radius: 4px;
      margin: 16px 0;
      border-left: 4px solid #2196f3;
    }

    .warning-text {
      color: #f44336;
      font-weight: 500;
      margin-top: 16px;
    }

    mat-dialog-content {
      min-width: 300px;
    }
  `]
})
export class DeleteCardTransactionDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<DeleteCardTransactionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: CardTransaction
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
