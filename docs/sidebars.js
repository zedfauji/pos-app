/**
 * Creating a sidebar enables you to:
 - create an ordered group of docs
 - render a sidebar for each doc of that group
 - provide next/previous navigation

 The sidebars can be generated from the filesystem, or explicitly defined here.

 Create as many sidebars as you want.
 */

// @ts-check

/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  // By default, Docusaurus generates a sidebar from the docs folder structure
  tutorialSidebar: [
    {
      type: 'doc',
      id: 'index',
      label: 'Home',
    },
    {
      type: 'category',
      label: 'Getting Started',
      items: [
        'getting-started/overview',
        'getting-started/prerequisites',
        'getting-started/installation',
        'getting-started/quick-start',
      ],
    },
    {
      type: 'category',
      label: 'Architecture',
      items: [
        'architecture/overview',
        'architecture/system-architecture',
        'architecture/database-architecture',
        'architecture/rbac-architecture',
      ],
    },
    {
      type: 'category',
      label: 'Operations & Support',
      items: [
        'operations/overview',
        'operations/operational-handbook',
        'operations/logging',
        'operations/monitoring',
        'operations/alerting',
        'operations/service-management',
        'operations/backup-restore',
        'operations/disaster-recovery',
        'operations/performance-tuning',
        {
          type: 'category',
          label: 'Runbooks',
          items: [
            'operations/runbooks/index',
            'operations/runbooks/health-check',
            'operations/runbooks/service-restart',
          ],
        },
        {
          type: 'category',
          label: 'Playbooks',
          items: [
            'operations/playbooks/index',
            'operations/playbooks/system-outage',
            'operations/playbooks/refund-processing',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: 'DevOps',
      items: [
        'devops/ci-cd',
      ],
    },
    {
      type: 'category',
      label: 'Testing',
      items: [
        'testing/overview',
      ],
    },
    {
      type: 'category',
      label: 'Contributing',
      items: [
        'contributing/overview',
      ],
    },
    {
      type: 'category',
      label: 'Frontend',
      items: [
        'frontend/overview',
        {
          type: 'category',
          label: 'ViewModels',
          items: [
            'frontend/viewmodels/overview',
            'frontend/viewmodels/users-viewmodel',
            'frontend/viewmodels/orders-viewmodel',
          ],
        },
        'frontend/printing',
        'frontend/pane-management',
      ],
    },
    {
      type: 'category',
      label: 'Backend',
      items: [
        'backend/overview',
        'backend/users-api',
        'backend/menu-api',
        'backend/order-api',
        'backend/payment-api',
        'backend/inventory-api',
        'backend/settings-api',
        'backend/customer-api',
        'backend/discount-api',
        'backend/tables-api',
      ],
    },
    {
      type: 'category',
      label: 'Features',
      items: [
        'features/features-explained',
        'features/rbac',
        'features/payments',
        'features/split-payments',
        'features/table-management',
        'features/printing',
        'features/vendor-management',
      ],
    },
    {
      type: 'category',
      label: 'API Reference',
      items: [
        'api/overview',
        'api/openapi',
      ],
    },
    {
      type: 'category',
      label: 'Security',
      items: [
        'security/rbac',
      ],
    },
    {
      type: 'category',
      label: 'Developer Guide',
      items: [
        'dev-guide/coding-standards',
      ],
    },
    {
      type: 'category',
      label: 'Troubleshooting',
      items: [
        'troubleshooting/common-issues',
      ],
    },
    {
      type: 'category',
      label: 'Changelog',
      items: [
        'changelog/releases',
        'changelog/template',
      ],
    },
    {
      type: 'doc',
      id: 'faq/index',
      label: 'FAQ',
    },
    {
      type: 'category',
      label: 'User Training Guides',
      items: [
        'user-guides/index',
        'user-guides/server-quick-start',
        'user-guides/server-training-manual',
        'user-guides/server-daily-tasks',
        'user-guides/manager-quick-start',
        'user-guides/manager-training-manual',
        'user-guides/manager-daily-tasks',
        {
          type: 'category',
          label: 'Getting Started',
          items: [
            'user-guides/getting-started/logging-in',
            'user-guides/getting-started/interface-overview',
            'user-guides/getting-started/navigation',
          ],
        },
        {
          type: 'category',
          label: 'Table Management',
          items: [
            'user-guides/table-management/starting-session',
            'user-guides/table-management/managing-tables',
            'user-guides/table-management/stopping-session',
            'user-guides/table-management/moving-tables',
          ],
        },
        {
          type: 'category',
          label: 'Orders',
          items: [
            'user-guides/orders/creating-order',
            'user-guides/orders/adding-items',
            'user-guides/orders/modifying-orders',
            'user-guides/orders/viewing-status',
          ],
        },
        {
          type: 'category',
          label: 'Payments',
          items: [
            'user-guides/payments/processing-payment',
            'user-guides/payments/split-payments',
            'user-guides/payments/applying-discounts',
            'user-guides/payments/processing-refunds',
            'user-guides/payments/printing-receipts',
          ],
        },
        {
          type: 'category',
          label: 'Quick Reference',
          items: [
            'user-guides/quick-reference/cheat-sheet',
            'user-guides/quick-reference/faq',
            'user-guides/quick-reference/shortcuts',
            'user-guides/quick-reference/troubleshooting',
          ],
        },
      ],
    },
  ],
};

module.exports = sidebars;
