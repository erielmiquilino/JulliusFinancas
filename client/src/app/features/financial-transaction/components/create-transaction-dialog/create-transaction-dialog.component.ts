import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { FinancialTransactionService, TransactionType } from '../../services/financial-transaction.service';

@Component({
  selector: 'app-create-transaction-dialog',
  templateUrl: './create-transaction-dialog.component.html',
  styleUrls: ['./create-transaction-dialog.component.scss']
})
export class CreateTransactionDialogComponent {
  form: FormGroup;
  TransactionType = TransactionType;
  transactionTypes = [
    { value: TransactionType.PayableBill, label: 'Conta a Pagar' },
    { value: TransactionType.ReceivableBill, label: 'Conta a Receber' }
  ];

  constructor(
    private fb: FormBuilder,
    private transactionService: FinancialTransactionService,
    private dialogRef: MatDialogRef<CreateTransactionDialogComponent>
  ) {
    const today = new Date();
    const localToday = new Date(today.getTime() + today.getTimezoneOffset() * 60000);

    this.form = this.fb.group({
      description: ['', Validators.required],
      amount: ['', [Validators.required, Validators.min(0.01)]],
      dueDate: [localToday, Validators.required],
      type: [TransactionType.PayableBill, Validators.required],
      isPaid: [false],
      isInstallment: [false],
      installmentCount: [1, [Validators.min(1), Validators.max(24)]]
    });

    // Observa mudanças no campo isInstallment para habilitar/desabilitar número de parcelas
    this.form.get('isInstallment')?.valueChanges.subscribe(isInstallment => {
      const installmentCountControl = this.form.get('installmentCount');
      if (isInstallment) {
        installmentCountControl?.setValidators([Validators.required, Validators.min(2), Validators.max(24)]);
        installmentCountControl?.setValue(2);
      } else {
        installmentCountControl?.setValidators([Validators.min(1), Validators.max(24)]);
        installmentCountControl?.setValue(1);
      }
      installmentCountControl?.updateValueAndValidity();
    });

    // Observa mudanças no campo tipo para ocultar parcelamento quando for conta a receber
    this.form.get('type')?.valueChanges.subscribe(type => {
      if (type === TransactionType.ReceivableBill) {
        // Se mudou para conta a receber, reseta o parcelamento
        this.form.get('isInstallment')?.setValue(false);
        this.form.get('installmentCount')?.setValue(1);
      }
    });
  }

  onSubmit(): void {
    if (this.form.valid) {
      const formValue = this.form.value;
      const dueDate = new Date(formValue.dueDate);
      const utcDueDate = new Date(dueDate.getTime() - dueDate.getTimezoneOffset() * 60000);

      this.transactionService.createTransaction({
        description: formValue.description,
        amount: formValue.amount,
        dueDate: utcDueDate,
        type: formValue.type,
        isPaid: formValue.isPaid,
        isInstallment: formValue.isInstallment,
        installmentCount: formValue.isInstallment ? formValue.installmentCount : 1
      })
        .subscribe({
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

  get isInstallment(): boolean {
    return this.form.get('isInstallment')?.value || false;
  }

  get isReceivableBill(): boolean {
    return this.form.get('type')?.value === TransactionType.ReceivableBill;
  }
}
