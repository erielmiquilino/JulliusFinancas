import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
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
import { Subject, takeUntil } from 'rxjs';
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
    CommonModule,
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
})
export class SettingsComponent implements OnInit, OnDestroy {
  private readonly configService = inject(BotConfigurationService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);
  private readonly destroy$ = new Subject<void>();

  configForm: FormGroup;

  saving: Record<string, boolean> = {};
  configStatus: Record<string, boolean> = {};
  testingTelegram = false;
  testingGemini = false;

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
    const controls: Record<string, FormControl> = {};
    [...this.telegramFields, ...this.geminiFields].forEach(field => {
      controls[field.key] = this.fb.control('');
      this.saving[field.key] = false;
      this.configStatus[field.key] = false;
    });
    this.configForm = this.fb.group(controls);
  }

  ngOnInit(): void {
    this.loadConfigurations();
    this.configService.refresh$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.loadConfigurations());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getFormControl(key: string): FormControl {
    return this.configForm.get(key) as FormControl;
  }

  getSectionStatus(section: 'telegram' | 'gemini'): SectionStatus {
    const fields = section === 'telegram' ? this.telegramFields : this.geminiFields;
    const configured = fields.filter(f => this.configStatus[f.key]).length;
    const total = fields.length;

    if (configured === total) {
      return { label: `${configured}/${total} configurados`, allConfigured: true, partial: false };
    }
    if (configured > 0) {
      return { label: `${configured}/${total} configurados`, allConfigured: false, partial: true };
    }
    return { label: `${configured}/${total} configurados`, allConfigured: false, partial: false };
  }

  loadConfigurations(): void {
    this.configService.getAll().subscribe({
      next: (configs) => {
        configs.forEach(config => {
          this.configStatus[config.configKey] = config.hasValue;
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

    this.saving[key] = true;
    this.configService.upsert(key, { value, description }).subscribe({
      next: () => {
        this.saving[key] = false;
        this.configStatus[key] = true;
        this.getFormControl(key).reset();
        this.showSuccess('Configuração salva com sucesso!');
      },
      error: () => {
        this.saving[key] = false;
        this.showError('Erro ao salvar configuração');
      },
    });
  }

  testTelegram(): void {
    this.testingTelegram = true;
    this.configService.testTelegram().subscribe({
      next: (result) => {
        this.testingTelegram = false;
        result.success
          ? this.showSuccess('Telegram conectado!')
          : this.showError(result.message);
      },
      error: (err) => {
        this.testingTelegram = false;
        this.showError(err.error?.message || 'Erro ao testar');
      },
    });
  }

  testGemini(): void {
    this.testingGemini = true;
    this.configService.testGemini().subscribe({
      next: (result) => {
        this.testingGemini = false;
        result.success
          ? this.showSuccess('Gemini conectado!')
          : this.showError(result.message);
      },
      error: (err) => {
        this.testingGemini = false;
        this.showError(err.error?.message || 'Erro ao testar');
      },
    });
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
