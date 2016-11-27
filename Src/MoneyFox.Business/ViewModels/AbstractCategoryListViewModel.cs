﻿using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using MoneyFox.Foundation.DataModels;
using MoneyFox.Foundation.Groups;
using MoneyFox.Foundation.Interfaces;
using MoneyFox.Foundation.Interfaces.Repositories;
using MoneyFox.Foundation.Resources;
using MvvmCross.Core.ViewModels;
using System;
using System.Diagnostics;

namespace MoneyFox.Business.ViewModels
{
    public abstract class AbstractCategoryListViewModel : BaseViewModel
    {
        protected readonly ICategoryRepository CategoryRepository;
        protected readonly IDialogService DialogService;

        private string searchText;
        private ObservableCollection<CategoryViewModel> categories;
        private CategoryViewModel selectedCategory;
        private ObservableCollection<AlphaGroupListGroup<CategoryListItemWrapper>> source;

        /// <summary>
        ///     Baseclass for the categorylist usercontrol
        /// </summary>
        /// <param name="categoryRepository">An instance of <see cref="IRepository{CategoryViewModel}" />.</param>
        /// <param name="dialogService">An instance of <see cref="IDialogService" /></param>
        protected AbstractCategoryListViewModel(ICategoryRepository categoryRepository,
            IDialogService dialogService)
        {
            DialogService = dialogService;
            CategoryRepository = categoryRepository;
        }

        /// <summary>
        ///     Handle the selection of a CategoryViewModel in the list
        /// </summary>
        protected abstract void ItemClick(CategoryViewModel category);

        #region Properties


        /// <summary>
        ///     Collection with all categories
        /// </summary>
        public ObservableCollection<CategoryViewModel> Categories
        {
            get { return categories; }
            set
            {
                if (categories == value) return;
                categories = value;
                RaisePropertyChanged();
                // ReSharper disable once ExplicitCallerInfoArgument
                RaisePropertyChanged(nameof(IsCategoriesEmpty));
            }
        }

        /// <summary>
        ///     Collection with categories alphanumeric grouped by
        /// </summary>
        public ObservableCollection<AlphaGroupListGroup<CategoryListItemWrapper>> Source
        {
            get { return source; }
            set
            {
                if (source == value) return;
                source = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     CategoryViewModel currently selected in the view.
        /// </summary>
        public CategoryViewModel SelectedCategory
        {
            get { return selectedCategory; }
            set
            {
                if (selectedCategory == value) return;
                selectedCategory = value;
                RaisePropertyChanged();
            }
        }

        public bool IsCategoriesEmpty => !Categories?.Any() ?? true;

        /// <summary>
        ///     Text to search for. Will perform the search when the text changes.
        /// </summary>
        public string SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                Search();
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Commands

        public MvxCommand LoadedCommand => new MvxCommand(Loaded);

        /// <summary>
        ///     Deletes the passed CategoryViewModel after show a confirmation dialog.
        /// </summary>
        public MvxCommand<CategoryViewModel> DeleteCategoryCommand => new MvxCommand<CategoryViewModel>(DeleteCategory);

        /// <summary>
        ///     Edit the currently selected CategoryViewModel
        /// </summary>
        public MvxCommand<CategoryViewModel> EditCategoryCommand => new MvxCommand<CategoryViewModel>(EditCategory);

        /// <summary>
        ///     Selects the clicked CategoryViewModel and sends it to the message hub.
        /// </summary>
        public MvxCommand<CategoryViewModel> ItemClickCommand => new MvxCommand<CategoryViewModel>(ItemClick);

        /// <summary>
        ///     Create and save a new CategoryViewModel group
        /// </summary>
        public MvxCommand<CategoryViewModel> CreateNewCategoryCommand
            => new MvxCommand<CategoryViewModel>(CreateNewCategory);

        #endregion

        /// <summary>
        ///     Performs a search with the text in the searchtext property
        /// </summary>
        public void Search()
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                Categories = new ObservableCollection<CategoryViewModel>
                    (CategoryRepository.GetList(
                        x => (x.Name != null) && x.Name.ToLower().Contains(searchText.ToLower()))
                        .OrderBy(x => x.Name));
            } else
            {
                Categories =
                    new ObservableCollection<CategoryViewModel>(CategoryRepository.GetList().OrderBy(x => x.Name));
            }
            Source = CreateGroup();
        }

        private void Loaded()
        {
            SearchText = string.Empty;
            Search();
        }

        private void EditCategory(CategoryViewModel category)
        {
            ShowViewModel<ModifyCategoryViewModel>(new {isEdit = true, selectedCategoryId = category.Id});
        }

        private void CreateNewCategory(CategoryViewModel category)
        {
            ShowViewModel<ModifyCategoryViewModel>(new {isEdit = false, SelectedCategory = 0});
        }

        private ObservableCollection<AlphaGroupListGroup<CategoryListItemWrapper>> CreateGroup() =>
            new ObservableCollection<AlphaGroupListGroup<CategoryListItemWrapper>>(
                AlphaGroupListGroup<CategoryListItemWrapper>.CreateGroups(Categories.Select(x => new CategoryListItemWrapper(x, ItemClickCommand)),
                    CultureInfo.CurrentUICulture,
                    s => string.IsNullOrEmpty(s.Item.Name)
                        ? "-"
                        : s.Item.Name[0].ToString().ToUpper()));

        private async void DeleteCategory(CategoryViewModel categoryToDelete)
        {
            if (await DialogService.ShowConfirmMessage(Strings.DeleteTitle, Strings.DeleteCategoryConfirmationMessage))
            {
                if (Categories.Contains(categoryToDelete))
                {
                    Categories.Remove(categoryToDelete);
                }

                CategoryRepository.Delete(categoryToDelete);
                Search();
            }
        }
    }

    public class CategoryListItemWrapper
    {
        public CategoryListItemWrapper(CategoryViewModel item, MvxCommand<CategoryViewModel> itemClickCommand)
        {
            Item = item;
            ItemClickCommand = new MvxCommand<CategoryListItemWrapper>(Foo);
        }

        private void Foo(CategoryListItemWrapper obj)
        {
            Debug.WriteLine("Foo");
        }

        public CategoryViewModel Item { get; }

        public MvxCommand<CategoryListItemWrapper> ItemClickCommand { get; }

    }
}