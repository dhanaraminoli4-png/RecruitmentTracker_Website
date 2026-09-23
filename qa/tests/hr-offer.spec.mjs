import { expect, test } from '@playwright/test';
import { readFile } from 'node:fs/promises';
import { registerCandidate, signIn } from '../helpers/auth.mjs';

function futureDate(days) {
  const date = new Date();
  date.setDate(date.getDate() + days);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

test('admin assigns HR, then HR publishes a vacancy and generates an offer PDF', async ({ browser, page }) => {
  const id = `${Date.now()}${Math.random().toString(36).slice(2, 7)}`;
  const email = `qa-hr-${id}@example.test`;
  const password = 'QATest#2026a';
  const title = `QA Engineer ${id}`;
  const templateName = `QA_Offer_${id}`;
  const baseURL = test.info().project.use.baseURL;

  await test.step('Register a candidate for promotion to HR', async () => {
    await registerCandidate(page, email, password);
  });

  const adminContext = await browser.newContext({ baseURL });
  const hrContext = await browser.newContext({ baseURL, acceptDownloads: true });

  try {
    const admin = await adminContext.newPage();
    await test.step('System Admin assigns the HR role', async () => {
      await signIn(admin, 'admin@recruitmenttracker.com', 'Admin@12345');
      await expect(admin).toHaveURL(/\/Dashboard\/SystemAdmin(?:\?|$)/);
      await admin.goto(`/Role/Index?search=${encodeURIComponent(email)}`);

      const userRow = admin.getByRole('row').filter({ hasText: email });
      await expect(userRow).toBeVisible();
      await userRow.locator('select[name="role"]').selectOption('HR');
      await userRow.getByRole('button', { name: 'Assign Role' }).click();

      await expect(admin.getByText('User role changed to HR successfully.')).toBeVisible();
      await admin.goto(`/Role/Index?search=${encodeURIComponent(email)}`);
      await expect(admin.getByRole('row').filter({ hasText: email }).locator('.role-badge')).toHaveText('HR');
    });

    const hr = await hrContext.newPage();
    await test.step('New HR account signs in and publishes a vacancy', async () => {
      await signIn(hr, email, password);
      await expect(hr).toHaveURL(/\/Dashboard\/HR(?:\?|$)/);

      await hr.goto('/Vacancy/Create');
      await hr.locator('#JobTitle').fill(title);
      await hr.locator('#Department').selectOption('Information Technology');
      await hr.locator('#Location').fill('Colombo');
      await hr.locator('#EmploymentType').selectOption('Full-Time');
      await hr.locator('#ClosingDate').fill(futureDate(45));
      await hr.locator('#Description').fill('QA-only vacancy created by the Playwright test.');
      await hr.locator('#Requirements').fill('Experience with automated browser testing.');
      await hr.getByRole('button', { name: 'Publish Vacancy' }).click();

      await expect(hr).toHaveURL(/\/Vacancy(?:\/Index)?(?:\?|$)/);
      await expect(hr.getByText(title, { exact: true })).toBeVisible();
    });

    await test.step('HR creates a template with automatic variables', async () => {
      await hr.goto('/OfferTemplate/Create');
      await hr.locator('#Name').fill(templateName);
      await hr.locator('#IsActive').selectOption('true');
      await hr.locator('#templateContent').fill('An offer without variables');
      await hr.getByRole('button', { name: 'Create Template' }).click();
      await expect(hr.getByText('Add at least one variable such as {{candidate_name}}.')).toBeVisible();

      await hr.locator('#templateContent').fill(`Dear {{candidate_name}},\nWe are pleased to offer you the ${title} position.`);
      await hr.getByRole('button', { name: 'Create Template' }).click();
      await expect(hr).toHaveURL(/\/OfferTemplate(?:\/Index)?(?:\?|$)/);
      await expect(hr.locator('.offer-row').filter({ hasText: templateName })).toBeVisible();
    });

    await test.step('HR fills the offer and downloads a real PDF', async () => {
      const offerRow = hr.locator('.offer-row').filter({ hasText: templateName });
      await offerRow.locator('button[title="Generate Offer"]').click();
      const modal = hr.locator('#generateOfferOverlay');
      await expect(modal).toHaveClass(/show/);
      await modal.locator('input[name="values[candidate_name]"]').fill('QA Candidate');
      await modal.getByRole('button', { name: /Preview Offer/ }).click();

      await expect(hr.getByRole('heading', { name: 'Offer Letter Preview' })).toBeVisible();
      await expect(hr.locator('.offer-document-content')).toContainText('Dear QA Candidate');
      await expect(hr.locator('.offer-document-content')).toContainText(title);
      await expect(hr.locator('.offer-document-content')).not.toContainText('{{candidate_name}}');

      const downloadPromise = hr.waitForEvent('download');
      await hr.getByRole('button', { name: /Download PDF/ }).click();
      const download = await downloadPromise;
      expect(download.suggestedFilename()).toBe(`${templateName}.pdf`);
      const bytes = await readFile(await download.path());
      expect(bytes.subarray(0, 5).toString()).toBe('%PDF-');
      expect(bytes.length).toBeGreaterThan(1000);
    });
  } finally {
    await adminContext.close();
    await hrContext.close();
  }
});
