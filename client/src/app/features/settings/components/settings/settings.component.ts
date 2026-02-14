import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormControl } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil } from 'rxjs';
import { BotConfigurationService, BotConfigurationDto } from '../../services/bot-configuration.service';

interface ConfigField {
  key: string;
  label: string;
  description: string;
  icon: string;
  placeholder: string;
  type: 'text' | 'password';
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
    MatSnackBarModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  template: `
    <div class="settings-container">
      <div class="settings-header">
        <mat-icon class="header-icon">settings</mat-icon>
        <div>
          <h1>Configurações</h1>
          <p class="subtitle">Configure a integração com Telegram e Google Gemini</p>
        </div>
      </div>

      <!-- Telegram Section -->
      <mat-card class="config-card">
        <mat-card-header>
          <mat-icon mat-card-avatar class="section-icon telegram-icon">send</mat-icon>
          <mat-card-title>Telegram Bot</mat-card-title>
          <mat-card-subtitle>Token do bot e configuração de acesso</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          @for (field of telegramFields; track field.key) {
            <div class="config-field">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ field.label }}</mat-label>
                <input matInput
                  [type]="showPasswords[field.key] ? 'text' : 'password'"
                  [placeholder]="field.placeholder"
                  [formControl]="getFormControl(field.key)"
                >
                <mat-icon matPrefix>{{ field.icon }}</mat-icon>
                <button mat-icon-button matSuffix (click)="toggleVisibility(field.key)"
                  [matTooltip]="showPasswords[field.key] ? 'Ocultar' : 'Mostrar'">
                  <mat-icon>{{ showPasswords[field.key] ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
                <mat-hint>{{ field.description }}</mat-hint>
              </mat-form-field>

              <div class="field-actions">
                <button mat-raised-button color="primary"
                  (click)="saveConfig(field.key, field.description)"
                  [disabled]="!getFormControl(field.key).value || saving[field.key]">
                  @if (saving[field.key]) {
                    <mat-spinner diameter="20"></mat-spinner>
                  } @else {
                    <mat-icon>save</mat-icon>
                  }
                  Salvar
                </button>
                <mat-icon class="status-icon"
                  [class.configured]="configStatus[field.key]"
                  [matTooltip]="configStatus[field.key] ? 'Configurado' : 'Não configurado'">
                  {{ configStatus[field.key] ? 'check_circle' : 'cancel' }}
                </mat-icon>
              </div>
            </div>
          }
        </mat-card-content>

        <mat-card-actions>
          <button mat-stroked-button color="primary" (click)="testTelegram()" [disabled]="testingTelegram">
            @if (testingTelegram) {
              <mat-spinner diameter="20"></mat-spinner>
            } @else {
              <mat-icon>check</mat-icon>
            }
            Testar Conexão Telegram
          </button>
        </mat-card-actions>
      </mat-card>

      <!-- Gemini Section -->
      <mat-card class="config-card">
        <mat-card-header>
          <mat-icon mat-card-avatar class="section-icon gemini-icon">auto_awesome</mat-icon>
          <mat-card-title>Google Gemini</mat-card-title>
          <mat-card-subtitle>Chave de API para inteligência artificial</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          @for (field of geminiFields; track field.key) {
            <div class="config-field">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>{{ field.label }}</mat-label>
                <input matInput
                  [type]="showPasswords[field.key] ? 'text' : 'password'"
                  [placeholder]="field.placeholder"
                  [formControl]="getFormControl(field.key)"
                >
                <mat-icon matPrefix>{{ field.icon }}</mat-icon>
                <button mat-icon-button matSuffix (click)="toggleVisibility(field.key)"
                  [matTooltip]="showPasswords[field.key] ? 'Ocultar' : 'Mostrar'">
                  <mat-icon>{{ showPasswords[field.key] ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
                <mat-hint>{{ field.description }}</mat-hint>
              </mat-form-field>

              <div class="field-actions">
                <button mat-raised-button color="primary"
                  (click)="saveConfig(field.key, field.description)"
                  [disabled]="!getFormControl(field.key).value || saving[field.key]">
                  @if (saving[field.key]) {
                    <mat-spinner diameter="20"></mat-spinner>
                  } @else {
                    <mat-icon>save</mat-icon>
                  }
                  Salvar
                </button>
                <mat-icon class="status-icon"
                  [class.configured]="configStatus[field.key]"
                  [matTooltip]="configStatus[field.key] ? 'Configurado' : 'Não configurado'">
                  {{ configStatus[field.key] ? 'check_circle' : 'cancel' }}
                </mat-icon>
              </div>
            </div>
          }
        </mat-card-content>

        <mat-card-actions>
          <button mat-stroked-button color="primary" (click)="testGemini()" [disabled]="testingGemini">
            @if (testingGemini) {
              <mat-spinner diameter="20"></mat-spinner>
            } @else {
              <mat-icon>check</mat-icon>
            }
            Testar Conexão Gemini
          </button>
        </mat-card-actions>
      </mat-card>

      <!-- Webhook Section -->
      <mat-card class="config-card">
        <mat-card-header>
          <mat-icon mat-card-avatar class="section-icon webhook-icon">webhook</mat-icon>
          <mat-card-title>Webhook</mat-card-title>
          <mat-card-subtitle>Registrar webhook do Telegram para receber mensagens</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <div class="config-field">
            <mat-form-field appearance="outline" class="full-width">
              <mat-label>URL Base</mat-label>
              <input matInput
                type="text"
                placeholder="https://seu-dominio.com"
                [formControl]="webhookUrlControl"
              >
              <mat-icon matPrefix>link</mat-icon>
              <mat-hint>URL pública onde a API está hospedada</mat-hint>
            </mat-form-field>
          </div>
        </mat-card-content>

        <mat-card-actions>
          <button mat-raised-button color="accent"
            (click)="registerWebhook()"
            [disabled]="!webhookUrlControl.value || registeringWebhook">
            @if (registeringWebhook) {
              <mat-spinner diameter="20"></mat-spinner>
            } @else {
              <mat-icon>cloud_upload</mat-icon>
            }
            Registrar Webhook
          </button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: 100%;
      overflow-y: auto;
      overflow-x: hidden;
    }

    .settings-container {
      max-width: 800px;
      margin: 0 auto;
      padding: 24px;
      padding-bottom: 48px;
    }

    .settings-header {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 32px;
    }

    .header-icon {
      font-size: 36px;
      width: 36px;
      height: 36px;
      color: var(--mat-primary, #6750a4);
    }

    .settings-header h1 {
      margin: 0;
      font-size: 28px;
      font-weight: 500;
    }

    .subtitle {
      margin: 4px 0 0;
      color: rgba(0, 0, 0, 0.6);
      font-size: 14px;
    }

    .config-card {
      margin-bottom: 24px;
      border-radius: 16px;
    }

    .section-icon {
      width: 40px !important;
      height: 40px !important;
      font-size: 24px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 50%;
      color: white;
    }

    .telegram-icon { background: #0088cc; }
    .gemini-icon { background: linear-gradient(135deg, #4285f4, #ea4335); }
    .webhook-icon { background: #43a047; }

    .config-field {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      margin-bottom: 16px;
    }

    .full-width {
      flex: 1;
    }

    .field-actions {
      display: flex;
      align-items: center;
      gap: 8px;
      padding-top: 4px;
    }

    .status-icon {
      color: #e53935;
      transition: color 0.3s;
    }

    .status-icon.configured {
      color: #43a047;
    }

    mat-card-actions {
      padding: 8px 16px 16px;
    }

    mat-spinner {
      display: inline-block;
      margin-right: 8px;
    }
  `]
})
export class SettingsComponent implements OnInit, OnDestroy {
  private configService = inject(BotConfigurationService);
  private snackBar = inject(MatSnackBar);
  private fb = inject(FormBuilder);
  private destroy$ = new Subject<void>();

  configForm: FormGroup;
  webhookUrlControl = this.fb.control('');

  showPasswords: Record<string, boolean> = {};
  saving: Record<string, boolean> = {};
  configStatus: Record<string, boolean> = {};
  testingTelegram = false;
  testingGemini = false;
  registeringWebhook = false;

  telegramFields: ConfigField[] = [
    {
      key: 'TelegramBotToken',
      label: 'Bot Token',
      description: 'Token obtido do @BotFather no Telegram',
      icon: 'key',
      placeholder: '123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11',
      type: 'password'
    },
    {
      key: 'TelegramAuthorizedChatId',
      label: 'Chat ID Autorizado',
      description: 'Seu Chat ID (envie /start para o bot para descobrir)',
      icon: 'person',
      placeholder: '123456789',
      type: 'password'
    },
    {
      key: 'TelegramWebhookSecret',
      label: 'Webhook Secret',
      description: 'Token secreto para validar requisições do webhook',
      icon: 'lock',
      placeholder: 'meu-secret-seguro-123',
      type: 'password'
    }
  ];

  geminiFields: ConfigField[] = [
    {
      key: 'GeminiApiKey',
      label: 'API Key',
      description: 'Chave obtida em aistudio.google.com',
      icon: 'vpn_key',
      placeholder: 'AIzaSy...',
      type: 'password'
    }
  ];

  constructor() {
    const controls: Record<string, any> = {};
    [...this.telegramFields, ...this.geminiFields].forEach(field => {
      controls[field.key] = [''];
      this.showPasswords[field.key] = false;
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

  toggleVisibility(key: string): void {
    this.showPasswords[key] = !this.showPasswords[key];
  }

  loadConfigurations(): void {
    this.configService.getAll().subscribe({
      next: (configs) => {
        configs.forEach(config => {
          this.configStatus[config.configKey] = config.hasValue;
        });
      },
      error: () => {
        this.snackBar.open('Erro ao carregar configurações', 'Fechar', { duration: 3000 });
      }
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
        this.snackBar.open('Configuração salva com sucesso!', 'Fechar', { duration: 3000 });
      },
      error: () => {
        this.saving[key] = false;
        this.snackBar.open('Erro ao salvar configuração', 'Fechar', { duration: 3000 });
      }
    });
  }

  testTelegram(): void {
    this.testingTelegram = true;
    this.configService.testTelegram().subscribe({
      next: (result) => {
        this.testingTelegram = false;
        this.snackBar.open(
          result.success ? '✅ Telegram conectado!' : `❌ ${result.message}`,
          'Fechar',
          { duration: 5000 }
        );
      },
      error: (err) => {
        this.testingTelegram = false;
        this.snackBar.open(`❌ ${err.error?.message || 'Erro ao testar'}`, 'Fechar', { duration: 5000 });
      }
    });
  }

  testGemini(): void {
    this.testingGemini = true;
    this.configService.testGemini().subscribe({
      next: (result) => {
        this.testingGemini = false;
        this.snackBar.open(
          result.success ? '✅ Gemini conectado!' : `❌ ${result.message}`,
          'Fechar',
          { duration: 5000 }
        );
      },
      error: (err) => {
        this.testingGemini = false;
        this.snackBar.open(`❌ ${err.error?.message || 'Erro ao testar'}`, 'Fechar', { duration: 5000 });
      }
    });
  }

  registerWebhook(): void {
    const url = this.webhookUrlControl.value;
    if (!url) return;

    this.registeringWebhook = true;
    this.configService.registerWebhook(url).subscribe({
      next: (result) => {
        this.registeringWebhook = false;
        this.snackBar.open(
          result.success ? '✅ Webhook registrado!' : `❌ ${result.message}`,
          'Fechar',
          { duration: 5000 }
        );
      },
      error: (err) => {
        this.registeringWebhook = false;
        this.snackBar.open(`❌ ${err.error?.message || 'Erro ao registrar'}`, 'Fechar', { duration: 5000 });
      }
    });
  }
}
