#!/bin/bash

# Test Runner Script for MagiDesk Settings API
# This script runs all unit and integration tests for the settings functionality

echo "Starting MagiDesk Settings API Test Suite..."
echo "=============================================="

# Set test environment variables
export ASPNETCORE_ENVIRONMENT=Test
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=magidesk_test;Username=test_user;Password=test_password;SSL Mode=Disable;"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to run tests and capture results
run_tests() {
    local project_path=$1
    local test_name=$2
    
    echo -e "${YELLOW}Running $test_name...${NC}"
    
    if dotnet test "$project_path" --verbosity normal --logger "console;verbosity=detailed" --collect:"XPlat Code Coverage"; then
        echo -e "${GREEN}✓ $test_name passed${NC}"
        return 0
    else
        echo -e "${RED}✗ $test_name failed${NC}"
        return 1
    fi
}

# Track test results
total_tests=0
passed_tests=0
failed_tests=0

# Run backend service tests
if [ -d "solution/backend/SettingsApi.Tests" ]; then
    total_tests=$((total_tests + 1))
    if run_tests "solution/backend/SettingsApi.Tests/SettingsApi.Tests.csproj" "Backend Service Tests"; then
        passed_tests=$((passed_tests + 1))
    else
        failed_tests=$((failed_tests + 1))
    fi
else
    echo -e "${RED}Backend test project not found${NC}"
    failed_tests=$((failed_tests + 1))
fi

# Run frontend ViewModel tests
if [ -d "solution/frontend/Tests" ]; then
    total_tests=$((total_tests + 1))
    if run_tests "solution/frontend/Tests/Tests.csproj" "Frontend ViewModel Tests"; then
        passed_tests=$((passed_tests + 1))
    else
        failed_tests=$((failed_tests + 1))
    fi
else
    echo -e "${RED}Frontend test project not found${NC}"
    failed_tests=$((failed_tests + 1))
fi

# Generate test report
echo ""
echo "=============================================="
echo "Test Results Summary"
echo "=============================================="
echo -e "Total Test Suites: $total_tests"
echo -e "${GREEN}Passed: $passed_tests${NC}"
echo -e "${RED}Failed: $failed_tests${NC}"

if [ $failed_tests -eq 0 ]; then
    echo -e "${GREEN}All tests passed! 🎉${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed. Please check the output above.${NC}"
    exit 1
fi

