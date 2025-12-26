# Entra Token CLI Documentation

This directory contains the Hugo-based documentation site for Entra Token CLI, powered by [Lotus Docs](https://github.com/colinwilson/lotusdocs).

## Deployment

This documentation is deployed to a separate repository: **[garrardkitchen/entratool-docs](https://github.com/garrardkitchen/entratool-docs)**

### Setup Instructions

1. **Create the separate docs repository:**
   ```bash
   # On GitHub, create a new repository: garrardkitchen/entratool-docs
   # Enable GitHub Pages in Settings → Pages → Source: GitHub Actions
   ```

2. **Configure DNS:**
   Add a CNAME record for your custom domain:
   ```
   entratool-docs.garrardkitchen.com → garrardkitchen.github.io
   ```

3. **Push documentation to docs repository:**
   ```bash
   # From the main entra-token-cli repository
   cd docs/learn
   
   # Initialize as separate git repository
   git init
   git add .
   git commit -m "Initial documentation site"
   
   # Add remote and push
   git remote add origin https://github.com/garrardkitchen/entratool-docs.git
   git branch -M main
   git push -u origin main
   ```

4. **Set up GitHub Actions workflow in entratool-docs repository:**
   
   Create `.github/workflows/hugo.yml`:
   ```yaml
   name: Deploy Hugo site to Pages

   on:
     push:
       branches:
         - main
     workflow_dispatch:

   permissions:
     contents: read
     pages: write
     id-token: write

   concurrency:
     group: "pages"
     cancel-in-progress: false

   defaults:
     run:
       shell: bash

   jobs:
     build:
       runs-on: ubuntu-latest
       env:
         HUGO_VERSION: 0.153.2
       steps:
         - name: Checkout
           uses: actions/checkout@v4
           with:
             fetch-depth: 0

         - name: Setup Go
           uses: actions/setup-go@v5
           with:
             go-version: '1.21'

         - name: Setup Hugo
           uses: peaceiris/actions-hugo@v3
           with:
             hugo-version: ${{ env.HUGO_VERSION }}
             extended: true

         - name: Download Hugo modules
           run: hugo mod get

         - name: Setup Pages
           id: pages
           uses: actions/configure-pages@v5

         - name: Build with Hugo
           env:
             HUGO_CACHEDIR: ${{ runner.temp }}/hugo_cache
             HUGO_ENVIRONMENT: production
           run: |
             hugo \
               --gc \
               --minify \
               --baseURL "${{ steps.pages.outputs.base_url }}/"

         - name: Upload artifact
           uses: actions/upload-pages-artifact@v3
           with:
             path: ./public

     deploy:
       environment:
         name: github-pages
         url: ${{ steps.deployment.outputs.page_url }}
       runs-on: ubuntu-latest
       needs: build
       steps:
         - name: Deploy to GitHub Pages
           id: deployment
           uses: actions/deploy-pages@v4
   ```

## Local Development

### Prerequisites

- Hugo Extended v0.153.0 or higher
- Go 1.21 or higher (for Hugo modules)

### Running Locally

```bash
cd docs/learn

# Download Hugo modules
hugo mod get

# Start development server
hugo server -D

# Open http://localhost:1313
```

### Building for Production

```bash
hugo --gc --minify
```

## Documentation Structure

```
content/
├── _index.md                          # Home page
└── docs/
    ├── _index.md                      # Docs landing page
    ├── getting-started/               # Installation, quickstart, tutorials
    ├── core-concepts/                 # Profiles, flows, scopes, storage
    ├── user-guide/                    # Managing profiles, tokens, usage
    │   ├── managing-profiles/
    │   ├── generating-tokens/
    │   └── working-with-tokens/
    ├── oauth-flows/                   # Detailed OAuth2 flow guides
    ├── certificates/                  # Certificate authentication
    ├── recipes/                       # Practical examples
    ├── reference/                     # Command reference
    │   └── commands/
    ├── platform-guides/               # Windows, macOS, Linux guides
    └── troubleshooting/               # Common issues and solutions
```

## Content Guidelines

### Frontmatter

Each page should include:

```yaml
---
title: "Page Title"
description: "Brief description"
weight: 10  # Lower numbers appear first in navigation
---
```

### Code Blocks

Use triple backticks with language identifiers:

````markdown
```bash
entratool get-token -p myprofile
```
````

### Links

Use relative links for internal pages:

```markdown
[Learn about profiles](/docs/core-concepts/profiles/)
```

### Alerts

Use Hugo shortcodes for alerts:

```markdown
{{% alert context="warning" %}}
This is a warning message.
{{% /alert %}}
```

## Theme

This site uses the [Lotus Docs](https://github.com/colinwilson/lotusdocs) theme via Hugo modules.

Configuration in `hugo.yaml`:
- Primary color: Microsoft Blue (#0078D4)
- Custom domain: entratool-docs.garrardkitchen.com
- Emoji support enabled
- Syntax highlighting with Monokai theme

## Maintenance

### Updating Hugo Modules

```bash
hugo mod get -u
hugo mod tidy
```

### Checking for Broken Links

```bash
hugo server
# Use a link checker tool or browser extension
```

## Troubleshooting

### Module Download Issues

If Hugo modules fail to download:

```bash
# Clear module cache
hugo mod clean

# Re-download
hugo mod get
```

### Build Failures

Check Hugo version:
```bash
hugo version
# Must be Extended version 0.153.0+
```

## Contributing

1. Make changes in the main `entra-token-cli` repository under `docs/learn/`
2. Test locally with `hugo server`
3. Commit and push to main repository
4. Manually sync to `entratool-docs` repository:
   ```bash
   cd docs/learn
   git add .
   git commit -m "Update documentation"
   git push
   ```

Or set up automatic sync with GitHub Actions in the main repository.
