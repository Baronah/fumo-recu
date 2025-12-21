using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaginationScript : MonoBehaviour
{
    [SerializeField] List<GameObject> PageContents;
    [SerializeField] Button NextPg, PrevPg;
    [SerializeField] TMP_Text PageCnt;

    public int currentPage = 0;
    public int totalPages => PageContents.Count;

    private void Start()
    {
        if (PageContents.Count <= 0)
        {
            PageContents.ForEach(page => page.SetActive(false));
            return;
        }

        NextPg.onClick.AddListener(NextPage);
        PrevPg.onClick.AddListener(PrevPage);

        UpdatePageContent();
    }

    void UpdatePageContent()
    {
        NextPg.gameObject.SetActive(HasNextPage());
        PrevPg.gameObject.SetActive(HasPrevPage());

        PageContents.ForEach(page => page.SetActive(false));
        PageContents[currentPage].SetActive(true);

        PageCnt.text = $"Page {currentPage + 1} / {totalPages}";
    }
    
    bool HasNextPage()
    {
        return currentPage + 1 < PageContents.Count;
    }

    bool HasPrevPage()
    {
        return currentPage - 1 >= 0;
    }

    public void NextPage()
    {
        if (currentPage + 1 >= PageContents.Count)
            return;
        currentPage += 1;
        UpdatePageContent();
    }

    public void PrevPage()
    {
        if (currentPage - 1 < 0)
            return;
        currentPage -= 1;
        UpdatePageContent();
    }
}
