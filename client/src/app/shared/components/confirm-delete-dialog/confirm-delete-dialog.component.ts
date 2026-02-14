import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';

export interface ConfirmDeleteDialogData {
  /** Nome da entidade sendo excluída (ex: "Budget", "a categoria", "o cartão") */
  entityName: string;

  /** Descrição/nome específico do item sendo excluído (mostrado em negrito azul) */
  itemDescription: string;

  /** Lista de detalhes a serem mostrados na info-box (opcional) */
  details?: Array<{ label: string; value: string }>;

  /** Mensagem de aviso adicional em vermelho (opcional) */
  warningMessage?: string;

  /** Para exclusão em lote: lista de nomes dos itens (opcional) */
  items?: string[];

  /** Número máximo de itens visíveis na lista (default: 5) */
  maxVisibleItems?: number;
}

@Component({
  selector: 'app-confirm-delete-dialog',
  template: `
    <h2 mat-dialog-title>Excluir {{ data.entityName }}</h2>
    <mat-dialog-content>
      @if (data.items && data.items.length > 0) {
        <!-- Exclusão em lote -->
        <p>Tem certeza que deseja excluir <strong>{{ data.items.length }}</strong> {{ data.entityName }}?</p>
        <ul class="items-list">
          @for (item of visibleItems; track item) {
            <li>{{ item }}</li>
          }
          @if (remainingCount > 0) {
            <li class="remaining-count">e mais {{ remainingCount }}...</li>
          }
        </ul>
      } @else {
        <!-- Exclusão individual -->
        <p>Tem certeza que deseja excluir {{ data.entityName }} <strong>{{ data.itemDescription }}</strong>?</p>
      }

      <!-- Details box (opcional) -->
      @if (data.details && data.details.length > 0) {
        <div class="details-box">
          @for (detail of data.details; track detail.label) {
            <div class="detail-item">
              <strong>{{ detail.label }}:</strong> {{ detail.value }}
            </div>
          }
        </div>
      }

      <!-- Warning message (opcional) -->
      @if (data.warningMessage) {
        <p class="warning-text">{{ data.warningMessage }}</p>
      }

      <!-- Texto fixo de ação irreversível -->
      <p class="irreversible-text">Esta ação não pode ser desfeita.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancelar</button>
      <button mat-raised-button color="warn" (click)="onConfirm()">Excluir</button>
    </mat-dialog-actions>
  `,
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule
  ],
  styles: [`
    :host {
      display: block;
    }

    mat-dialog-content {
      min-width: 320px;
      max-width: 400px;
      padding: 0 24px;
      margin: 16px 0;
      overflow-y: visible;
    }

    h2[mat-dialog-title] {
      margin: 0;
      padding: 16px 24px;
    }

    p {
      margin: 0 0 12px 0;
      opacity: 0.9;
      word-wrap: break-word;
    }

    strong {
      color: #64b5f6;
      word-wrap: break-word;
    }

    .details-box {
      background-color: #f5f5f5;
      padding: 16px;
      border-radius: 4px;
      margin: 16px 0;
      border-left: 4px solid #2196f3;
      word-wrap: break-word;
    }

    .detail-item {
      margin-bottom: 8px;
      line-height: 1.5;
      word-wrap: break-word;
    }

    .detail-item:last-child {
      margin-bottom: 0;
    }

    .detail-item strong {
      color: rgba(0, 0, 0, 0.87);
    }

    .warning-text {
      color: #f44336;
      font-weight: 500;
      margin-top: 16px;
      word-wrap: break-word;
    }

    .irreversible-text {
      color: #f44336;
      font-size: 12px;
      margin-top: 8px;
      margin-bottom: 0;
      font-weight: 400;
    }

    .items-list {
      margin: 8px 0 16px 0;
      padding-left: 20px;
    }

    .items-list li {
      margin: 4px 0;
      color: rgba(0, 0, 0, 0.7);
      word-wrap: break-word;
    }

    .items-list .remaining-count {
      color: rgba(0, 0, 0, 0.6);
      font-style: italic;
    }

    mat-dialog-actions {
      padding: 16px 24px;
      margin: 0;
      min-height: 52px;
    }

    mat-dialog-actions button {
      margin-left: 8px;
    }

    mat-dialog-actions button:first-child {
      margin-left: 0;
    }
  `]
})
export class ConfirmDeleteDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<ConfirmDeleteDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ConfirmDeleteDialogData
  ) {}

  get visibleItems(): string[] {
    if (!this.data.items) return [];
    const maxVisible = this.data.maxVisibleItems || 5;
    return this.data.items.slice(0, maxVisible);
  }

  get remainingCount(): number {
    if (!this.data.items) return 0;
    const maxVisible = this.data.maxVisibleItems || 5;
    return Math.max(0, this.data.items.length - maxVisible);
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
