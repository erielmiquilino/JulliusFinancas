import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Card } from '../../services/card.service';

@Component({
  selector: 'app-delete-card-dialog',
  template: `
    <h2 mat-dialog-title>Confirmar Exclusão</h2>
    <mat-dialog-content>
      <p>Tem certeza que deseja excluir o cartão "{{ data.name }}"?</p>
      <div class="card-details">
        <strong>Banco Emissor:</strong> {{ data.issuingBank }}<br>
        <strong>Limite:</strong> {{ data.limit | currency:'BRL':'symbol':'1.2-2':'pt-BR' }}<br>
        <strong>Dia de Fechamento:</strong> Dia {{ data.closingDay }}
      </div>
      <p class="warning-text">Esta ação não pode ser desfeita. Todos os lançamentos associados a este cartão também serão excluídos.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancelar</button>
      <button mat-raised-button color="warn" (click)="onConfirm()">Excluir</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .card-details {
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
      min-width: 350px;
    }
  `]
})
export class DeleteCardDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<DeleteCardDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: Card
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
