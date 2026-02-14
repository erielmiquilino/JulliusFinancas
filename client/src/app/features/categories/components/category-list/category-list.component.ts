import { Component, OnInit, OnDestroy, ViewChild, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatExpansionModule } from '@angular/material/expansion';
import { BreakpointObserver } from '@angular/cdk/layout';
import { Subscription, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CategoryService, Category } from '../../services/category.service';
import { CreateCategoryDialogComponent } from '../create-category-dialog/create-category-dialog.component';
import { EditCategoryDialogComponent } from '../edit-category-dialog/edit-category-dialog.component';
import { ConfirmDeleteDialogComponent } from '../../../../shared/components/confirm-delete-dialog/confirm-delete-dialog.component';

@Component({
  selector: 'app-category-list',
  templateUrl: './category-list.component.html',
  styleUrls: ['./category-list.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatSortModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatInputModule,
    MatFormFieldModule,
    MatExpansionModule,
    DatePipe
  ]
})
export class CategoryListComponent implements OnInit, OnDestroy, AfterViewInit {
  private destroy$ = new Subject<void>();
  private readonly desktopColumns: string[] = ['color', 'name', 'createdAt', 'actions'];
  private readonly mobileColumns: string[] = ['color', 'name', 'actions'];

  displayedColumns: string[] = this.desktopColumns;
  dataSource: MatTableDataSource<Category>;
  private refreshSubscription: Subscription;
  isMobile = false;
  textFilter = '';

  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private categoryService: CategoryService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef,
    private breakpointObserver: BreakpointObserver
  ) {
    this.dataSource = new MatTableDataSource<Category>();
    this.dataSource.filterPredicate = this.createFilter();
    this.refreshSubscription = this.categoryService.refresh$.subscribe(() => {
      this.loadCategories();
    });
  }

  ngOnInit(): void {
    this.loadCategories();

    // Observa mudanças de breakpoint para ajustar colunas
    this.breakpointObserver
      .observe(['(max-width: 768px)'])
      .pipe(takeUntil(this.destroy$))
      .subscribe(result => {
        this.isMobile = result.matches;
        this.displayedColumns = this.isMobile ? this.mobileColumns : this.desktopColumns;
        this.cdr.detectChanges();
      });
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.dataSource.sort = this.sort;
      if (this.sort) {
        this.sort.active = 'name';
        this.sort.direction = 'asc';
      }
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy(): void {
    if (this.refreshSubscription) {
      this.refreshSubscription.unsubscribe();
    }
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCategories(): void {
    this.categoryService.getAllCategories().subscribe({
      next: (categories) => {
        this.dataSource.data = categories;
        if (this.sort) {
          this.dataSource.sort = this.sort;
        }
      },
      error: (error) => {
        console.error('Erro ao carregar categorias:', error);
        this.snackBar.open('Erro ao carregar categorias: ' + error.message, 'Fechar', {
          duration: 5000
        });
      }
    });
  }

  openCreateDialog(): void {
    const dialogRef = this.dialog.open(CreateCategoryDialogComponent, {
      width: '400px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Categoria criada com sucesso!', 'Fechar', {
          duration: 3000
        });
      }
    });
  }

  openEditDialog(category: Category): void {
    const dialogRef = this.dialog.open(EditCategoryDialogComponent, {
      width: '400px',
      disableClose: true,
      data: category
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Categoria atualizada com sucesso!', 'Fechar', {
          duration: 3000
        });
      }
    });
  }

  openDeleteDialog(category: Category): void {
    const dialogRef = this.dialog.open(ConfirmDeleteDialogComponent, {
      width: '400px',
      data: {
        entityName: 'a categoria',
        itemDescription: category.name
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.categoryService.deleteCategory(category.id).subscribe({
          next: () => {
            this.snackBar.open('Categoria excluída com sucesso!', 'Fechar', {
              duration: 3000
            });
          },
          error: (error) => {
            console.error('Erro ao excluir categoria:', error);
            const message = error.error || 'Erro ao excluir categoria';
            this.snackBar.open(message, 'Fechar', {
              duration: 5000
            });
          }
        });
      }
    });
  }

  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value;
    this.dataSource.filter = filterValue.trim().toLowerCase();
  }

  private createFilter(): (category: Category, filter: string) => boolean {
    return (category: Category, filter: string): boolean => {
      const searchStr = filter.toLowerCase();
      return category.name.toLowerCase().includes(searchStr);
    };
  }
}

