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
        'architecture/frontend-architecture',
        'architecture/backend-architecture',
        'architecture/database-architecture',
        'architecture/deployment-architecture',
        'architecture/rbac-architecture',
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
            'frontend/viewmodels/payment-viewmodel',
            'frontend/viewmodels/menu-viewmodel',
            'frontend/viewmodels/inventory-viewmodel',
          ],
        },
        {
          type: 'category',
          label: 'Views & Pages',
          items: [
            'frontend/views/overview',
            'frontend/views/dashboard-page',
            'frontend/views/orders-page',
            'frontend/views/payment-page',
            'frontend/views/menu-management',
          ],
        },
        {
          type: 'category',
          label: 'Services',
          items: [
            'frontend/services/overview',
            'frontend/services/api-services',
            'frontend/services/business-services',
            'frontend/services/utility-services',
          ],
        },
        'frontend/navigation',
        'frontend/data-binding',
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
      label: 'Database',
      items: [
        'database/overview',
        'database/schemas',
        'database/relationships',
        'database/migrations',
      ],
    },
    {
      type: 'category',
      label: 'Features',
      items: [
        'features/rbac',
        'features/payments',
        'features/orders',
        'features/inventory',
        'features/customers',
        'features/settings',
      ],
    },
    {
      type: 'category',
      label: 'API Reference',
      items: [
        'api/overview',
        'api/authentication',
        {
          type: 'category',
          label: 'API v1',
          items: [
            'api/v1/overview',
            'api/v1/users',
            'api/v1/menu',
            'api/v1/orders',
            'api/v1/payments',
          ],
        },
        {
          type: 'category',
          label: 'API v2',
          items: [
            'api/v2/overview',
            'api/v2/users',
            'api/v2/menu',
            'api/v2/orders',
            'api/v2/payments',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: 'Configuration',
      items: [
        'configuration/appsettings',
        'configuration/environment-variables',
        'configuration/deployment-config',
      ],
    },
    {
      type: 'category',
      label: 'Deployment',
      items: [
        'deployment/overview',
        'deployment/local-development',
        'deployment/cloud-run',
        'deployment/production',
      ],
    },
    {
      type: 'category',
      label: 'Security',
      items: [
        'security/overview',
        'security/rbac',
        'security/authentication',
        'security/best-practices',
      ],
    },
    {
      type: 'category',
      label: 'Developer Guide',
      items: [
        'dev-guide/coding-standards',
        'dev-guide/winui3-guidelines',
        'dev-guide/api-development',
        'dev-guide/testing',
      ],
    },
    {
      type: 'category',
      label: 'Troubleshooting',
      items: [
        'troubleshooting/common-issues',
        'troubleshooting/debugging',
        'troubleshooting/logs',
      ],
    },
    {
      type: 'category',
      label: 'Extending',
      items: [
        'extending/custom-features',
        'extending/integrations',
      ],
    },
    {
      type: 'doc',
      id: 'changelog/releases',
      label: 'Changelog',
    },
    {
      type: 'doc',
      id: 'faq/index',
      label: 'FAQ',
    },
  ],
};

module.exports = sidebars;
