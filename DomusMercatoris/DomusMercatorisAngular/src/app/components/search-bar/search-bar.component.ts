import { Component, ViewChild, ElementRef, inject, OnDestroy, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductService } from '../../services/product.service';
import { SearchService } from '../../services/search.service';
import { Subject, takeUntil } from 'rxjs';

// Extend Window interface to include heic2any if needed, but module import is better.
// If types are missing, declare them here.
declare module 'heic2any';
@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search-bar.component.html',
  styleUrl: './search-bar.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchBarComponent implements OnInit, OnDestroy {
  public productService = inject(ProductService);
  private searchService = inject(SearchService);
  private router = inject(Router);
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  query = '';
  isClassifying = false;
  selectedClusterId: number | null = null;
  classifyError: string | null = null;
  itemsPerPage = 9;
  showHint = false;
  panelOpen = false;
  isDragging = false;
  isExpanded = false;
  private readonly MAX_SIZE_BYTES = 17 * 1024 * 1024;
  private destroy$ = new Subject<void>();
  private destroyed = false;

  ngOnInit() {
    this.productService.fetchCategories();
  }

  ngOnDestroy() {
    this.destroyed = true;
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleSearch() {
    this.isExpanded = !this.isExpanded;
    if (this.isExpanded) {
      setTimeout(() => {
        const input = document.querySelector('.search-bar-container input') as HTMLInputElement;
        if (input) input.focus();
      }, 100);
    }
  }


  goSearch() {
    const q = this.query?.trim();
    if (!q || q.length === 0) return;

    // Reset filters for global search but keep category if selected
    this.productService.selectedCompany.set(null);
    this.productService.selectedBrand.set(null);
    
    const categoryId = this.productService.selectedCategory();

    this.searchService.searchProductsByName(q, 1, this.itemsPerPage, null, null, categoryId);
    this.router.navigate(['/products/search']);
  }

  togglePanel() {
    this.panelOpen = !this.panelOpen;
  }

  openSelectDialog() {
    if (this.fileInput) this.fileInput.nativeElement.click();
  }

  private async handleFileProcessing(file: File) {
    const validation = this.searchService.validateFile(file);
    if (!validation.valid) {
      this.classifyError = validation.error || 'Invalid file';
      return;
    }

    this.isClassifying = true; // Show loading
    this.classifyError = null;

    try {
      const processedFile = await this.searchService.processImage(file);
      
      this.searchService.classifyImage(processedFile)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            if (this.destroyed) return;
            this.selectedClusterId = res.clusterId;
            this.searchService.fetchProductsByCluster(res.clusterId, 1, this.itemsPerPage, this.productService.selectedCompany(), this.productService.selectedBrand());
            this.isClassifying = false;
            this.panelOpen = false; // Close the panel on success
            this.router.navigate(['/products/search']);
          },
          error: (err) => {
            if (this.destroyed) return;
            console.error('Classification error:', err);
            this.classifyError = 'Classification failed.';
            this.isClassifying = false;
          }
        });
    } catch (err) {
      if (this.destroyed) return;
      console.error('Processing error:', err);
      this.classifyError = 'Could not process image.';
      this.isClassifying = false;
    }
  }

  async onImageSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    
    // Reset input value to allow selecting same file again
    input.value = '';
    
    await this.handleFileProcessing(file);
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;
  }

  async onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;
    if (!event.dataTransfer || event.dataTransfer.files.length === 0) return;
    const file = event.dataTransfer.files[0];
    await this.handleFileProcessing(file);
  }
}
