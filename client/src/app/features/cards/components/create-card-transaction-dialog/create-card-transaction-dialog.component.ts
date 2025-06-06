import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { CardService } from '../../services/card.service';

@Component({
  selector: 'app-create-card-transaction-dialog',
  templateUrl: './create-card-transaction-dialog.component.html',
  styleUrls: ['./create-card-transaction-dialog.component.scss']
})
export class CreateCardTransactionDialogComponent {
  form: FormGroup;
  cardId: string;

  constructor(
    private fb: FormBuilder,
    private cardService: CardService,
    private dialogRef: MatDialogRef<CreateCardTransactionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { cardId: string }
  ) {
    this.cardId = data.cardId;

    const today = new Date();
    const localToday = new Date(today.getTime() + today.getTimezoneOffset() * 60000);

    this.form = this.fb.group({
      descricao: ['', [Validators.required, Validators.maxLength(100)]],
      valor: ['', [Validators.required, Validators.min(0.01)]],
      data: [localToday, Validators.required],
      parcelado: [false],
      numeroParcelas: [1, [Validators.min(1), Validators.max(24)]]
    });

    // Observa mudanças no campo parcelado para habilitar/desabilitar número de parcelas
    this.form.get('parcelado')?.valueChanges.subscribe(isParcelado => {
      const numeroParcelasControl = this.form.get('numeroParcelas');
      if (isParcelado) {
        numeroParcelasControl?.setValidators([Validators.required, Validators.min(2), Validators.max(24)]);
        numeroParcelasControl?.setValue(2);
      } else {
        numeroParcelasControl?.setValidators([Validators.min(1), Validators.max(24)]);
        numeroParcelasControl?.setValue(1);
      }
      numeroParcelasControl?.updateValueAndValidity();
    });
  }

  onSubmit(): void {
    if (this.form.valid) {
      const formValue = this.form.value;
      const data = new Date(formValue.data);
      const utcData = new Date(data.getTime() - data.getTimezoneOffset() * 60000);

      const isParcelado = formValue.parcelado;
      const numeroParcelas = isParcelado ? formValue.numeroParcelas : 1;

      // Se for parcelado, cria múltiplas transações
      if (isParcelado && numeroParcelas > 1) {
        this.createParceledTransactions(formValue, utcData, numeroParcelas);
      } else {
        // Cria uma única transação
        this.createSingleTransaction(formValue, utcData, '1/1');
      }
    }
  }

  private createSingleTransaction(formValue: any, data: Date, parcela: string): void {
    this.cardService.createCardTransaction({
      cardId: this.cardId,
      descricao: formValue.descricao,
      valor: formValue.valor,
      data: data,
      parcela: parcela
    }).subscribe({
      next: () => {
        this.dialogRef.close(true);
      },
      error: (error) => {
        console.error('Erro ao criar lançamento:', error);
      }
    });
  }

  private createParceledTransactions(formValue: any, dataInicial: Date, numeroParcelas: number): void {
    const transactions: any[] = [];

    // Cria as transações para cada parcela
    for (let i = 0; i < numeroParcelas; i++) {
      const dataVencimento = new Date(dataInicial);
      dataVencimento.setMonth(dataVencimento.getMonth() + i);

      transactions.push({
        cardId: this.cardId,
        descricao: formValue.descricao,
        valor: formValue.valor,
        data: dataVencimento,
        parcela: `${i + 1}/${numeroParcelas}`
      });
    }

    // Cria todas as transações em sequência
    this.createTransactionsSequentially(transactions, 0);
  }

  private createTransactionsSequentially(transactions: any[], index: number): void {
    if (index >= transactions.length) {
      // Todas as transações foram criadas
      this.dialogRef.close(true);
      return;
    }

    this.cardService.createCardTransaction(transactions[index]).subscribe({
      next: () => {
        // Cria a próxima transação
        this.createTransactionsSequentially(transactions, index + 1);
      },
      error: (error) => {
        console.error('Erro ao criar lançamento parcelado:', error);
        // Continua mesmo com erro para tentar criar as demais
        this.createTransactionsSequentially(transactions, index + 1);
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  get isParcelado(): boolean {
    return this.form.get('parcelado')?.value || false;
  }
}
