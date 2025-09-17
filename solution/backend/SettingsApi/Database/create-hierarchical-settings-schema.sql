-- Create settings schema for hierarchical settings storage
CREATE SCHEMA IF NOT EXISTS settings;

-- Drop existing tables if they exist (for clean recreation)
DROP TABLE IF EXISTS settings.settings_audit CASCADE;
DROP TABLE IF EXISTS settings.hierarchical_settings CASCADE;

-- Main hierarchical settings table
CREATE TABLE settings.hierarchical_settings (
    id BIGSERIAL PRIMARY KEY,
    host_key VARCHAR(100) NOT NULL DEFAULT 'default',
    category VARCHAR(50) NOT NULL,
    settings_json JSONB NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_by VARCHAR(100),
    updated_by VARCHAR(100),
    
    -- Ensure unique combination of host_key and category
    CONSTRAINT uk_hierarchical_settings_host_category UNIQUE (host_key, category)
);

-- Settings audit log table
CREATE TABLE settings.settings_audit (
    id BIGSERIAL PRIMARY KEY,
    host_key VARCHAR(100) NOT NULL DEFAULT 'default',
    action VARCHAR(50) NOT NULL,
    description TEXT,
    category VARCHAR(50),
    changes_json JSONB,
    changed_by VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ip_address INET,
    user_agent TEXT
);

-- Create indexes for performance
CREATE INDEX idx_hierarchical_settings_host_key ON settings.hierarchical_settings (host_key);
CREATE INDEX idx_hierarchical_settings_category ON settings.hierarchical_settings (category);
CREATE INDEX idx_hierarchical_settings_active ON settings.hierarchical_settings (is_active) WHERE is_active = true;
CREATE INDEX idx_hierarchical_settings_updated ON settings.hierarchical_settings (updated_at DESC);

CREATE INDEX idx_settings_audit_host_key ON settings.settings_audit (host_key);
CREATE INDEX idx_settings_audit_created_at ON settings.settings_audit (created_at DESC);
CREATE INDEX idx_settings_audit_action ON settings.settings_audit (action);
CREATE INDEX idx_settings_audit_category ON settings.settings_audit (category);

-- Create GIN index for JSONB queries
CREATE INDEX idx_hierarchical_settings_json ON settings.hierarchical_settings USING GIN (settings_json);
CREATE INDEX idx_settings_audit_changes_json ON settings.settings_audit USING GIN (changes_json);

-- Function to automatically update the updated_at timestamp
CREATE OR REPLACE FUNCTION settings.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Trigger to automatically update updated_at
CREATE TRIGGER trigger_update_hierarchical_settings_updated_at
    BEFORE UPDATE ON settings.hierarchical_settings
    FOR EACH ROW
    EXECUTE FUNCTION settings.update_updated_at_column();

-- Insert default settings for common categories
INSERT INTO settings.hierarchical_settings (host_key, category, settings_json) VALUES 
('default', 'general', '{
    "businessName": "MagiDesk POS",
    "businessAddress": "123 Business Street, City, State 12345",
    "businessPhone": "+1-555-0123",
    "businessEmail": null,
    "businessWebsite": null,
    "theme": "System",
    "language": "en-US",
    "timezone": "America/New_York",
    "hostKey": null
}'),

('default', 'pos', '{
    "cashDrawer": {
        "autoOpenOnSale": true,
        "autoOpenOnRefund": true,
        "comPort": null,
        "baudRate": 9600,
        "requireManagerOverride": false
    },
    "tableLayout": {
        "ratePerMinute": 0.50,
        "warnAfterMinutes": 30,
        "autoStopAfterMinutes": 120,
        "enableAutoStop": false,
        "showTimerOnTables": true
    },
    "shifts": {
        "shiftStartTime": "09:00:00",
        "shiftEndTime": "21:00:00",
        "requireShiftReports": true,
        "autoCloseShift": false
    },
    "tax": {
        "defaultTaxRate": 8.5,
        "taxInclusivePricing": false,
        "taxDisplayName": "Tax",
        "additionalTaxRates": []
    }
}'),

('default', 'inventory', '{
    "stock": {
        "lowStockThreshold": 10,
        "criticalStockThreshold": 5,
        "enableStockAlerts": true,
        "autoDeductStock": true,
        "allowNegativeStock": false
    },
    "reorder": {
        "enableAutoReorder": false,
        "reorderLeadTimeDays": 7,
        "safetyStockMultiplier": 1.5
    },
    "vendors": {
        "defaultPaymentTerms": 30,
        "defaultCurrency": "USD",
        "requireApprovalForOrders": true
    }
}'),

('default', 'customers', '{
    "membership": {
        "tiers": [],
        "defaultExpiryDays": 365,
        "autoRenewMemberships": false,
        "requireEmailForMembership": true
    },
    "wallet": {
        "enableWalletSystem": true,
        "maxWalletBalance": 1000.0,
        "minTopUpAmount": 10.0,
        "allowNegativeBalance": false,
        "requireIdForWalletUse": false
    },
    "loyalty": {
        "enableLoyaltyProgram": true,
        "pointsPerDollar": 1.0,
        "pointValue": 0.01,
        "minPointsForRedemption": 100
    }
}'),

('default', 'payments', '{
    "enabledMethods": [
        {
            "name": "Cash",
            "type": "Cash",
            "isEnabled": true,
            "surchargePercentage": null,
            "minAmount": null,
            "maxAmount": null
        },
        {
            "name": "Credit Card",
            "type": "Card",
            "isEnabled": true,
            "surchargePercentage": null,
            "minAmount": null,
            "maxAmount": null
        },
        {
            "name": "Debit Card",
            "type": "Card",
            "isEnabled": true,
            "surchargePercentage": null,
            "minAmount": null,
            "maxAmount": null
        }
    ],
    "discounts": {
        "maxDiscountPercentage": 50.0,
        "requireManagerApproval": true,
        "allowStackingDiscounts": false,
        "presetDiscounts": ["5%", "10%", "15%", "20%"]
    },
    "surcharges": {
        "enableSurcharges": false,
        "cardSurchargePercentage": 2.5,
        "showSurchargeOnReceipt": true
    },
    "splitPayments": {
        "enableSplitPayments": true,
        "maxSplitCount": 4,
        "allowUnevenSplits": true
    }
}'),

('default', 'printers', '{
    "receipt": {
        "defaultPrinter": "",
        "fallbackPrinter": "",
        "paperSize": "80mm",
        "autoPrintOnPayment": true,
        "previewBeforePrint": true,
        "printProForma": true,
        "printFinalReceipt": true,
        "copiesForFinalReceipt": 2,
        "printCustomerCopy": true,
        "printMerchantCopy": true,
        "template": {
            "showLogo": true,
            "showBusinessInfo": true,
            "showItemDetails": true,
            "showTaxBreakdown": true,
            "showPaymentMethod": true,
            "footerMessage": "Thank you for your business!",
            "headerMessage": "",
            "topMargin": 5,
            "bottomMargin": 5,
            "leftMargin": 2,
            "rightMargin": 2
        }
    },
    "kitchen": {
        "printers": [],
        "printOrderNumbers": true,
        "printTimestamps": true,
        "printSpecialInstructions": true,
        "copiesPerOrder": 1
    },
    "devices": {
        "availablePrinters": [],
        "autoDetectPrinters": true,
        "defaultComPort": "COM1",
        "defaultBaudRate": 9600
    },
    "jobs": {
        "maxRetries": 3,
        "timeoutMs": 5000,
        "logPrintJobs": true,
        "queueFailedJobs": true,
        "maxQueueSize": 50
    }
}'),

('default', 'notifications', '{
    "email": {
        "enableEmail": false,
        "smtpServer": "",
        "smtpPort": 587,
        "username": "",
        "password": "",
        "useSsl": true,
        "fromAddress": "",
        "fromName": ""
    },
    "sms": {
        "enableSms": false,
        "provider": "",
        "apiKey": "",
        "apiSecret": "",
        "fromNumber": ""
    },
    "push": {
        "enablePush": true,
        "showStockAlerts": true,
        "showOrderAlerts": true,
        "showPaymentAlerts": true,
        "showSystemAlerts": true
    },
    "alerts": {
        "thresholdAlerts": [],
        "enableSoundAlerts": true,
        "alertSoundPath": "",
        "alertVolume": 50
    }
}'),

('default', 'security', '{
    "rbac": {
        "enforceRolePermissions": true,
        "allowRoleInheritance": true,
        "requireManagerOverride": true,
        "restrictedOperations": []
    },
    "login": {
        "maxLoginAttempts": 5,
        "lockoutDurationMinutes": 15,
        "requireStrongPasswords": true,
        "passwordExpiryDays": 90,
        "enableTwoFactor": false
    },
    "sessions": {
        "sessionTimeoutMinutes": 60,
        "enableAutoLogout": true,
        "requireReauthForSensitive": true,
        "maxConcurrentSessions": 3
    },
    "audit": {
        "enableAuditLogging": true,
        "logUserActions": true,
        "logSystemEvents": true,
        "logDataChanges": true,
        "retentionDays": 365
    }
}'),

('default', 'integrations', '{
    "paymentGateways": {
        "gateways": [],
        "defaultGateway": "",
        "enableTestMode": false
    },
    "webhooks": {
        "endpoints": [],
        "enableWebhooks": false,
        "timeoutMs": 10000,
        "maxRetries": 3
    },
    "crm": {
        "enableCrmSync": false,
        "crmProvider": "",
        "apiEndpoint": "",
        "apiKey": "",
        "syncCustomers": true,
        "syncOrders": true,
        "syncIntervalMinutes": 60
    },
    "api": {
        "endpoints": [
            {
                "name": "MenuApi",
                "baseUrl": "https://magidesk-menu-904541739138.northamerica-south1.run.app",
                "isEnabled": true,
                "apiKey": null,
                "timeoutMs": 10000,
                "maxRetries": 3
            },
            {
                "name": "OrderApi",
                "baseUrl": "https://magidesk-order-904541739138.northamerica-south1.run.app",
                "isEnabled": true,
                "apiKey": null,
                "timeoutMs": 10000,
                "maxRetries": 3
            },
            {
                "name": "PaymentApi",
                "baseUrl": "https://magidesk-payment-904541739138.northamerica-south1.run.app",
                "isEnabled": true,
                "apiKey": null,
                "timeoutMs": 10000,
                "maxRetries": 3
            },
            {
                "name": "InventoryApi",
                "baseUrl": "https://magidesk-inventory-904541739138.northamerica-south1.run.app",
                "isEnabled": true,
                "apiKey": null,
                "timeoutMs": 10000,
                "maxRetries": 3
            },
            {
                "name": "TablesApi",
                "baseUrl": "https://magidesk-tables-904541739138.northamerica-south1.run.app",
                "isEnabled": true,
                "apiKey": null,
                "timeoutMs": 10000,
                "maxRetries": 3
            },
            {
                "name": "UsersApi",
                "baseUrl": "https://magidesk-users-23sbzjsxaq-pv.a.run.app",
                "isEnabled": true,
                "apiKey": null,
                "timeoutMs": 10000,
                "maxRetries": 3
            },
            {
                "name": "SettingsApi",
                "baseUrl": "https://magidesk-settings-904541739138.us-central1.run.app",
                "isEnabled": true,
                "apiKey": null,
                "timeoutMs": 10000,
                "maxRetries": 3
            },
            {
                "name": "VendorOrdersApi",
                "baseUrl": "https://magidesk-vendororders-904541739138.northamerica-south1.run.app",
                "isEnabled": true,
                "apiKey": null,
                "timeoutMs": 10000,
                "maxRetries": 3
            }
        ],
        "defaultTimeoutMs": 10000,
        "defaultRetries": 3,
        "enableApiLogging": true
    }
}'),

('default', 'system', '{
    "logging": {
        "logLevel": "Information",
        "enableFileLogging": true,
        "enableConsoleLogging": true,
        "logFilePath": "logs/magidesk.log",
        "maxLogFileSizeMB": 10,
        "maxLogFiles": 10,
        "logRetentionDays": 30
    },
    "tracing": {
        "enableTracing": false,
        "traceApiCalls": true,
        "traceDbQueries": false,
        "traceUserActions": true,
        "tracingEndpoint": ""
    },
    "backgroundJobs": {
        "enableBackgroundJobs": true,
        "heartbeatIntervalMinutes": 5,
        "cleanupIntervalMinutes": 60,
        "backupIntervalMinutes": 240
    },
    "performance": {
        "databaseConnectionPoolSize": 50,
        "cacheExpirationMinutes": 15,
        "enableResponseCompression": true,
        "enableResponseCaching": true,
        "maxConcurrentRequests": 1000
    }
}');

-- Insert initial audit entry
INSERT INTO settings.settings_audit (host_key, action, description, created_at) 
VALUES ('default', 'schema_created', 'Hierarchical settings schema created with default values', NOW());

-- Grant permissions (adjust as needed for your security model)
-- GRANT USAGE ON SCHEMA settings TO pos_app_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA settings TO pos_app_user;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA settings TO pos_app_user;

COMMENT ON SCHEMA settings IS 'Hierarchical settings storage for MagiDesk POS system';
COMMENT ON TABLE settings.hierarchical_settings IS 'Main table for storing hierarchical settings organized by category';
COMMENT ON TABLE settings.settings_audit IS 'Audit log for all settings changes';
COMMENT ON COLUMN settings.hierarchical_settings.settings_json IS 'JSONB column containing the actual settings data for the category';
COMMENT ON COLUMN settings.hierarchical_settings.host_key IS 'Identifier for the specific installation/host (allows multi-tenant usage)';
COMMENT ON COLUMN settings.hierarchical_settings.category IS 'Settings category (general, pos, inventory, customers, payments, printers, notifications, security, integrations, system)';
