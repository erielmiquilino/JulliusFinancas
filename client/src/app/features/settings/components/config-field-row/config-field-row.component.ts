import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-config-field-row',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <div class="field-row">
      <mat-form-field appearance="outline" class="field-input">
        <mat-label>{{ label }}</mat-label>
        <input matInput
          [type]="showValue ? 'text' : inputType"
          [placeholder]="placeholder"
          [formControl]="control"
        >
        <mat-icon matPrefix class="field-prefix-icon">{{ icon }}</mat-icon>
        @if (inputType === 'password') {
          <button mat-icon-button matSuffix
            (click)="toggleVisibility()" type="button"
            [matTooltip]="showValue ? 'Ocultar' : 'Mostrar'">
            <mat-icon>{{ showValue ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>
        }
        <mat-hint>{{ description }}</mat-hint>
      </mat-form-field>

      <div class="field-actions">
        <button mat-flat-button class="save-btn"
          (click)="save.emit()"
          [disabled]="!control.value || isSaving">
          @if (isSaving) {
            <mat-spinner diameter="18"></mat-spinner>
          } @else {
            <mat-icon>save</mat-icon>
          }
          Salvar
        </button>

        <span class="status-indicator"
          [class.configured]="isConfigured"
          [matTooltip]="isConfigured ? 'Configurado' : 'Não configurado'">
          <mat-icon>{{ isConfigured ? 'check_circle' : 'cancel' }}</mat-icon>
        </span>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }

    .field-row {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px 0;
    }

    .field-row:not(:last-of-type) {
      border-bottom: 1px solid rgba(0, 0, 0, 0.05);
    }

    .field-input {
      flex: 1;
      min-width: 0;
    }

    .field-prefix-icon {
      color: var(--text-secondary, rgba(0, 0, 0, 0.54));
    }

    .field-actions {
      display: flex;
      align-items: center;
      gap: 8px;
      padding-top: 4px;
      flex-shrink: 0;
    }

    .save-btn {
      white-space: nowrap;

      mat-spinner {
        display: inline-flex;
        margin-right: 6px;
      }
    }

    .status-indicator {
      display: inline-flex;
      align-items: center;
      color: #d32f2f;
      transition: color 0.3s ease, transform 0.3s ease;

      mat-icon {
        font-size: 22px;
        width: 22px;
        height: 22px;
      }

      &.configured {
        color: #2e7d32;
        animation: pop-in 0.3s ease;
      }
    }

    @keyframes pop-in {
      0%   { transform: scale(0.8); }
      50%  { transform: scale(1.15); }
      100% { transform: scale(1); }
    }

    @media (max-width: 599px) {
      .field-row {
        flex-direction: column;
        gap: 8px;
      }

      .field-actions {
        align-self: flex-end;
      }
    }
  `],
})
export class ConfigFieldRowComponent {
  @Input() label = '';
  @Input() description = '';
  @Input() icon = '';
  @Input() placeholder = '';
  @Input() control!: FormControl;
  @Input() isConfigured = false;
  @Input() isSaving = false;
  @Input() inputType: 'text' | 'password' = 'password';

  @Output() save = new EventEmitter<void>();

  showValue = false;

  toggleVisibility(): void {
    this.showValue = !this.showValue;
  }
}
