import { Component, ViewChild, ElementRef, inject, OnDestroy, OnInit } from '@angular/core';
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
  styleUrl: './search-bar.component.css'
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

  private validateFile(file: File): boolean {
    // iOS fix: sometimes type is empty or specific formats
    const isImageMime = file.type && file.type.startsWith('image/');
    const validExtensions = /\.(jpg|jpeg|png|webp|gif|bmp|heic|heif)$/i;
    const hasValidExtension = validExtensions.test(file.name);

    // Accept if it has image mime type OR valid extension
    // Also accept if type is empty (iOS camera sometimes) but we trust the input accept="image/*"
    // But to be safe, we rely on extension if type is missing.
    // If both missing, we might reject, but let's be permissive for "image.jpg" with empty type.
    
    if (!isImageMime && !hasValidExtension) {
       // If type is empty, maybe it's a raw camera capture without extension?
       // But usually it has one. Let's log for debugging if we could.
       // For now, if type is present but NOT image, reject.
       if (file.type && !file.type.startsWith('image/')) {
         this.classifyError = 'Only images are accepted.';
         return false;
       }
       // If type is empty and no extension, it's suspicious but might be valid on some devices.
       // Let's allow it if size is reasonable, backend will handle it.
       if (!file.type && !hasValidExtension) {
           // Allow it for now to fix iOS issues
       }
    }

    if (file.size > this.MAX_SIZE_BYTES) {
      this.classifyError = 'Image exceeds 17MB limit.';
      return false;
    }
    return true;
  }

  private async handleFileProcessing(file: File) {
    if (!this.validateFile(file)) return;

    // Convert HEIC if needed
    if (file.name.toLowerCase().endsWith('.heic') || file.name.toLowerCase().endsWith('.heif')) {
      try {
        this.isClassifying = true; // Show loading during conversion
        
        // Dynamic import for optimization
        const heic2anyModule = await import('heic2any');
        const heic2any = heic2anyModule.default || heic2anyModule;
        
        const convertedBlob = await heic2any({
          blob: file,
          toType: 'image/jpeg',
          quality: 0.8
        });
        
        // heic2any can return Blob or Blob[]
        const blob = Array.isArray(convertedBlob) ? convertedBlob[0] : convertedBlob;
        file = new File([blob], file.name.replace(/\.hei[cf]$/i, '.jpg'), { type: 'image/jpeg' });
      } catch (err) {
        if (this.destroyed) return;
        console.error('HEIC conversion failed:', err);
        this.classifyError = 'Could not process HEIC image.';
        this.isClassifying = false;
        return;
      }
    }
    
    this.isClassifying = true;
    this.classifyError = null;
    this.searchService.classifyImage(file).pipe(takeUntil(this.destroy$)).subscribe({
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
