import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormControl } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';
import { BotConfigurationService } from '../../services/bot-configuration.service';
import { ConfigFieldRowComponent } from '../config-field-row/config-field-row.component';

interface ConfigField {
  key: string;
  label: string;
  description: string;
  icon: string;
  placeholder: string;
  type: 'text' | 'password';
}

interface SectionStatus {
  label: string;
  allConfigured: boolean;
  partial: boolean;
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    ConfigFieldRowComponent,
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsComponent {
  private readonly configService = inject(BotConfigurationService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly configForm: FormGroup;

  /** Reactive state — new reference on every update guarantees CD under OnPush */
  readonly saving = signal<Record<string, boolean>>({});
  readonly configStatus = signal<Record<string, boolean>>({});
  readonly testingTelegram = signal(false);
  readonly testingGemini = signal(false);

  /** Derived state — recomputed only when configStatus signal changes */
  readonly telegramStatus = computed(() => this.computeSectionStatus('telegram'));
  readonly geminiStatus = computed(() => this.computeSectionStatus('gemini'));

  readonly telegramFields: ConfigField[] = [
    {
      key: 'TelegramBotToken',
      label: 'Bot Token',
      description: 'Token obtido do @BotFather no Telegram',
      icon: 'key',
      placeholder: '123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11',
      type: 'password',
    },
    {
      key: 'TelegramAuthorizedChatId',
      label: 'Chat ID Autorizado',
      description: 'Seu Chat ID (envie /start para o bot para descobrir)',
      icon: 'person',
      placeholder: '123456789',
      type: 'password',
    },
    {
      key: 'TelegramWebhookSecret',
      label: 'Webhook Secret',
      description: 'Token secreto para validar requisições do webhook',
      icon: 'lock',
      placeholder: 'meu-secret-seguro-123',
      type: 'password',
    },
  ];

  readonly geminiFields: ConfigField[] = [
    {
      key: 'GeminiApiKey',
      label: 'API Key',
      description: 'Chave obtida em aistudio.google.com',
      icon: 'vpn_key',
      placeholder: 'AIzaSy...',
      type: 'password',
    },
  ];

  constructor() {
    // Build form controls and initial signal state
    const controls: Record<string, FormControl> = {};
    const initialSaving: Record<string, boolean> = {};
    const initialStatus: Record<string, boolean> = {};

    [...this.telegramFields, ...this.geminiFields].forEach(field => {
      controls[field.key] = this.fb.control('');
      initialSaving[field.key] = false;
      initialStatus[field.key] = false;
    });

    this.configForm = this.fb.group(controls);
    this.saving.set(initialSaving);
    this.configStatus.set(initialStatus);

    // Initial load
    this.loadConfigurations();

    // Re-load whenever a config is upserted/deleted elsewhere
    this.configService.refresh$
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.loadConfigurations());
  }

  getFormControl(key: string): FormControl {
    return this.configForm.get(key) as FormControl;
  }

  loadConfigurations(): void {
    this.configService.getAll().subscribe({
      next: (configs) => {
        this.configStatus.update(current => {
          const updated = { ...current };
          configs.forEach(config => {
            updated[config.configKey] = config.hasValue;
          });
          return updated;
        });
      },
      error: () => {
        this.showError('Erro ao carregar configurações');
      },
    });
  }

  saveConfig(key: string, description: string): void {
    const value = this.getFormControl(key).value;
    if (!value) return;

    this.saving.update(s => ({ ...s, [key]: true }));

    this.configService
      .upsert(key, { value, description })
      .pipe(finalize(() => this.saving.update(s => ({ ...s, [key]: false }))))
      .subscribe({
        next: () => {
          this.configStatus.update(s => ({ ...s, [key]: true }));
          this.getFormControl(key).reset();
          this.showSuccess('Configuração salva com sucesso!');
        },
        error: () => {
          this.showError('Erro ao salvar configuração');
        },
      });
  }

  testTelegram(): void {
    this.testingTelegram.set(true);

    this.configService
      .testTelegram()
      .pipe(finalize(() => this.testingTelegram.set(false)))
      .subscribe({
        next: (result) => {
          result.success
            ? this.showSuccess('Telegram conectado!')
            : this.showError(result.message);
        },
        error: (err) => {
          this.showError(err.error?.message || 'Erro ao testar');
        },
      });
  }

  testGemini(): void {
    this.testingGemini.set(true);

    this.configService
      .testGemini()
      .pipe(finalize(() => this.testingGemini.set(false)))
      .subscribe({
        next: (result) => {
          result.success
            ? this.showSuccess('Gemini conectado!')
            : this.showError(result.message);
        },
        error: (err) => {
          this.showError(err.error?.message || 'Erro ao testar');
        },
      });
  }

  private computeSectionStatus(section: 'telegram' | 'gemini'): SectionStatus {
    const fields = section === 'telegram' ? this.telegramFields : this.geminiFields;
    const status = this.configStatus();
    const configured = fields.filter(f => status[f.key]).length;
    const total = fields.length;

    if (configured === total) {
      return { label: `${configured}/${total} configurados`, allConfigured: true, partial: false };
    }
    if (configured > 0) {
      return { label: `${configured}/${total} configurados`, allConfigured: false, partial: true };
    }
    return { label: `${configured}/${total} configurados`, allConfigured: false, partial: false };
  }

  private showSuccess(message: string): void {
    this.snackBar.open(`✅ ${message}`, 'Fechar', {
      duration: 4000,
      panelClass: 'success-snackbar',
    });
  }

  private showError(message: string): void {
    this.snackBar.open(`❌ ${message}`, 'Fechar', {
      duration: 5000,
      panelClass: 'error-snackbar',
    });
  }
}
