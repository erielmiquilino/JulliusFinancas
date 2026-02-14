import { Component, Inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { Budget } from '../../services/budget.service';

@Component({
  selector: 'app-edit-budget-dialog',
  templateUrl: './edit-budget-dialog.component.html',
  styleUrls: ['./edit-budget-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule
  ]
})
export class EditBudgetDialogComponent implements OnInit {
  form: FormGroup;

  months = [
    { value: 1, label: 'Janeiro' },
    { value: 2, label: 'Fevereiro' },
    { value: 3, label: 'Março' },
    { value: 4, label: 'Abril' },
    { value: 5, label: 'Maio' },
    { value: 6, label: 'Junho' },
    { value: 7, label: 'Julho' },
    { value: 8, label: 'Agosto' },
    { value: 9, label: 'Setembro' },
    { value: 10, label: 'Outubro' },
    { value: 11, label: 'Novembro' },
    { value: 12, label: 'Dezembro' }
  ];

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<EditBudgetDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: Budget
  ) {
    this.form = this.fb.group({
      name: [data.name, [Validators.required, Validators.maxLength(100)]],
      limitAmount: [data.limitAmount, [Validators.required, Validators.min(0.01)]],
      description: [data.description || '', [Validators.maxLength(500)]],
      month: [data.month, Validators.required],
      year: [data.year, [Validators.required, Validators.min(2000), Validators.max(2100)]]
    });
  }

  ngOnInit(): void { }

  onSave(): void {
    if (this.form.valid) {
      const formValue = this.form.value;
      this.dialogRef.close({
        name: formValue.name,
        limitAmount: formValue.limitAmount,
        description: formValue.description || undefined,
        month: formValue.month,
        year: formValue.year
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}

