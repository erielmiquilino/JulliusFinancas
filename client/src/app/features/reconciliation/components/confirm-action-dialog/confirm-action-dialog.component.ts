import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/**
 * Diálogo de confirmação genérico da conciliação.
 *
 * O ConfirmDeleteDialogComponent compartilhado é amarrado a exclusão — título "Excluir {entidade}",
 * botão vermelho "Excluir" e o texto fixo "Esta ação não pode ser desfeita". Usá-lo para confirmar
 * o lançamento dizia ao usuário exatamente o oposto do que o botão fazia.
 */
export interface ConfirmActionDialogData {
  title: string;
  message: string;
  details?: Array<{ label: string; value: string }>;
  warningMessage?: string;
  confirmLabel: string;
  confirmColor?: 'primary' | 'warn';
  confirmIcon?: string;
}

@Component({
  selector: 'app-confirm-action-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>

      @if (data.details && data.details.length > 0) {
        <div class="details-box">
          @for (detail of data.details; track detail.label) {
            <div class="detail-item">
              <strong>{{ detail.label }}:</strong> {{ detail.value }}
            </div>
          }
        </div>
      }

      @if (data.warningMessage) {
        <p class="warning-text">{{ data.warningMessage }}</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancelar</button>
      <button mat-raised-button [color]="data.confirmColor || 'primary'" (click)="onConfirm()">
        @if (data.confirmIcon) {
          <mat-icon>{{ data.confirmIcon }}</mat-icon>
        }
        {{ data.confirmLabel }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .details-box {
      background-color: #f5f5f5;
      border-left: 4px solid var(--primary-color, #2e7d32);
      border-radius: 4px;
      padding: 12px;
      margin: 12px 0;
    }

    .detail-item {
      font-size: 13px;
      padding: 2px 0;
    }

    .warning-text {
      color: #e65100;
      font-size: 13px;
      margin-top: 8px;
    }
  `]
})
export class ConfirmActionDialogComponent {
  constructor(
    private dialogRef: MatDialogRef<ConfirmActionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmActionDialogData
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
