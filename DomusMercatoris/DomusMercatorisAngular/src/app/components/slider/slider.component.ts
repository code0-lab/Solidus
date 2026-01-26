import { Component, inject, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { BannerService } from '../../services/banner.service';
import { Banner } from '../../models/banner.model';

@Component({
  selector: 'app-slider',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './slider.component.html',
  styleUrl: './slider.component.css'
})
export class SliderComponent implements OnInit {
  private bannerService = inject(BannerService);
  private sanitizer = inject(DomSanitizer);

  banner = computed<Banner | null>(() => this.bannerService.banner());

  safeHtml = computed<SafeHtml | null>(() => {
    // If API banner exists, use it. Otherwise, use DEFAULT DESIGN.
    let content = this.banner()?.htmlContent;

    if (!content) {
      content = `
        <div class="default-banner">
          <div class="crt-overlay"></div>
          <div class="content">
            <h1 class="glitch">SOLIDUS<span class="cursor">_</span></h1>
            <p>SYSTEM OPTIMIZED // READY FOR COMMERCE</p>
            <div class="decoration">
              <span>=====</span>
              <span>EST. 2024</span>
              <span>=====</span>
            </div>
            <button class="action-btn">BROWSE CATALOG</button>
          </div>
        </div>
        <style>
          .default-banner {
            width: 100%;
            height: 100%;
            background-color: #f0f0f0;
            background-image: 
              linear-gradient(45deg, #000 25%, transparent 25%, transparent 75%, #000 75%, #000), 
              linear-gradient(45deg, #000 25%, transparent 25%, transparent 75%, #000 75%, #000);
            background-size: 4px 4px;
            background-position: 0 0, 2px 2px;
            display: flex;
            justify-content: center;
            align-items: center;
            position: relative;
            font-family: "DotGothic16", monospace;
          }
          /* Overlay to lighten the dither pattern */
          .default-banner::before {
            content: "";
            position: absolute;
            inset: 0;
            background: rgba(255, 255, 255, 0.9);
            z-index: 0;
          }
          .content {
            position: relative;
            z-index: 1;
            text-align: center;
            border: 4px solid black;
            padding: 2rem 4rem;
            background: white;
            box-shadow: 8px 8px 0 black;
            max-width: 80%;
          }
          h1 {
            font-size: 3rem;
            margin: 0;
            letter-spacing: 4px;
            line-height: 1;
            color: black;
          }
          p {
            font-size: 1.2rem;
            margin: 10px 0 20px;
            color: #555;
            font-weight: bold;
          }
          .cursor {
            animation: blink 1s step-end infinite;
          }
          @keyframes blink { 50% { opacity: 0; } }
          
          .decoration {
            display: flex;
            gap: 20px;
            justify-content: center;
            margin-bottom: 20px;
            font-size: 14px;
            color: black;
          }
          
          .action-btn {
            background: #0000FF;
            color: white;
            border: none;
            padding: 10px 20px;
            font-family: inherit;
            font-size: 1.2rem;
            cursor: pointer;
            box-shadow: 4px 4px 0 #888;
            transition: transform 0.1s, box-shadow 0.1s;
          }
          .action-btn:active {
            transform: translate(2px, 2px);
            box-shadow: 2px 2px 0 #888;
          }
        </style>
      `;
    }

    const html = `
      <!DOCTYPE html>
      <html lang="en">
      <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <style>
          @import url('https://fonts.googleapis.com/css2?family=DotGothic16&display=swap');
          body {
            margin: 0;
            padding: 0;
            width: 100vw;
            height: 100vh;
            overflow: hidden;
            display: flex;
            justify-content: center;
            align-items: center;
            font-family: "DotGothic16", sans-serif;
          }
          /* Ensure all children are responsive */
          * {
            max-width: 100%;
            box-sizing: border-box;
          }
        </style>
      </head>
      <body>
        ${content}
      </body>
      </html>
    `;
    return this.sanitizer.bypassSecurityTrustHtml(html);
  });

  ngOnInit() {
    this.bannerService.loadActiveBanner();
  }
}
