# Payment Flow Refactoring Plan

## Issues Identified and Solutions

### 1. Session/Billing ID Resolution
**Problem**: Complex fallback logic causing inconsistent state
**Solution**: Implement robust ID resolution service

### 2. TablesApi Dependency
**Problem**: Payment system dependent on TablesApi availability
**Solution**: Make TablesApi validation optional with fallback

### 3. Error Handling
**Problem**: Generic exception handling losing error details
**Solution**: Implement structured error handling with detailed logging

### 4. Split Payment Logic
**Problem**: Tips/discounts not properly distributed in split payments
**Solution**: Implement proper tip/discount distribution algorithm

### 5. Database Schema
**Problem**: Inconsistent ID types (bigint vs uuid)
**Solution**: Standardize on UUID for all payment-related IDs

## Implementation Priority
1. Fix error handling and logging
2. Implement robust ID resolution
3. Fix split payment logic
4. Make TablesApi dependency optional
5. Update database schema
