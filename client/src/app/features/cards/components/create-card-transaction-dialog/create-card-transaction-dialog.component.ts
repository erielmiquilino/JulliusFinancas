import { Component, Inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { CardService, CreateCardTransactionRequest } from '../../services/card.service';

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
      description: ['', [Validators.required, Validators.maxLength(100)]],
      amount: ['', [Validators.required, Validators.min(0.01)]],
      date: [localToday, Validators.required],
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
      const data = new Date(formValue.date);
      const utcData = new Date(data.getTime() - data.getTimezoneOffset() * 60000);

      const transaction: CreateCardTransactionRequest = {
        cardId: this.data.cardId,
        description: formValue.description,
        amount: formValue.amount,
        date: utcData,
        isInstallment: formValue.parcelado,
        installmentCount: formValue.parcelado ? formValue.numeroParcelas : 1
      };

      this.cardService.createCardTransaction(transaction).subscribe({
        next: (response) => {
          console.log('Transação(ões) criada(s) com sucesso:', response);
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Erro ao criar transação:', error);
        }
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  get isParcelado(): boolean {
    return this.form.get('parcelado')?.value || false;
  }
}
