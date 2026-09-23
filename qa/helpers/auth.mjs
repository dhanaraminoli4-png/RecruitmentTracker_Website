import { expect } from '@playwright/test';

export async function registerCandidate(page, email, password = 'QATest#2026a') {
  await page.goto('/Identity/Account/Register');
  await page.locator('#Input_Email').fill(email);
  await page.locator('#Input_Password').fill(password);
  await page.locator('#Input_ConfirmPassword').fill(password);
  await page.locator('#registerSubmit').click();
  await expect(page).toHaveURL(/\/Dashboard\/Candidate(?:\?|$)/);
}

export async function signIn(page, email, password) {
  await page.goto('/Identity/Account/Login');
  await page.locator('#Input_Email').fill(email);
  await page.locator('#Input_Password').fill(password);
  await page.locator('#login-submit').click();
}
