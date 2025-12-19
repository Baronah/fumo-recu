using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaginationScript : MonoBehaviour
{
    [SerializeField] List<string> PageContents;
    [SerializeField] TMP_Text pageContentLeft, pageContentRight;
    [SerializeField] Button NextPg, PrevPg;

    public int currentPage = 0;
    public int totalPages
    {
        get
        {
            return (PageContents.Count + 1) / 2;
        }
    }

    private void Start()
    {
        if (PageContents.Count <= 0)
        {
            pageContentLeft.text = "No Content";
            pageContentRight.text = "";
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

        pageContentLeft.text = PageContents[currentPage].Replace(@"\n","\n");
        if (currentPage + 1 < PageContents.Count)
            pageContentRight.text = PageContents[currentPage + 1].Replace(@"\n", "\n");
        else
            pageContentRight.text = "";
    }
    
    bool HasNextPage()
    {
        return currentPage + 2 < PageContents.Count;
    }

    bool HasPrevPage()
    {
        return currentPage - 2 >= 0;
    }

    public void NextPage()
    {
        if (currentPage + 2 >= PageContents.Count)
            return;
        currentPage += 2;
        UpdatePageContent();
    }

    public void PrevPage()
    {
        if (currentPage - 2 < 0)
            return;
        currentPage -= 2;
        UpdatePageContent();
    }
}
